using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Isthifa.AspNetCore.ExceptionHandling.Exceptions;
using Isthifa.AspNetCore.ExceptionHandling.Extensions;
using Isthifa.AspNetCore.ExceptionHandling.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Isthifa.AspNetCore.ExceptionHandling.Tests;

/// <summary>Integration tests for the exception-handling middleware.</summary>
public sealed class ExceptionHandlingIntegrationTests
{
    public static TheoryData<Exception, HttpStatusCode> CustomExceptions => new()
    {
        { new BadRequestException("Invalid request"), HttpStatusCode.BadRequest },
        { new UnauthorizedException(), HttpStatusCode.Unauthorized },
        { new ForbiddenException(), HttpStatusCode.Forbidden },
        { new ResourceNotFoundException("Missing"), HttpStatusCode.NotFound },
        { new ConflictException("Duplicate"), HttpStatusCode.Conflict },
        { new ValidationException("Validation failed", [new FieldViolation("name", "Required")]), HttpStatusCode.BadRequest },
        { new ApiException(422, "Unprocessable"), (HttpStatusCode)422 },
    };

    [Theory]
    [MemberData(nameof(CustomExceptions))]
    public async Task Custom_exceptions_return_their_status(Exception exception, HttpStatusCode status)
    {
        using var server = CreateServer(app => app.Run(_ => Task.FromException(exception)));
        using var response = await server.CreateClient().GetAsync("/");
        Assert.Equal(status, response.StatusCode);
    }

    [Fact]
    public async Task Response_has_the_standard_contract_and_trace_id()
    {
        using var server = CreateServer(app => app.Run(_ => throw new ResourceNotFoundException("Customer 42 was not found", "CUSTOMER_NOT_FOUND")));
        var response = await server.CreateClient().GetAsync("/");
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error = Assert.IsType<ApiErrorResponse>(error);
        Assert.Equal(404, error.Status);
        Assert.Equal("Not Found", error.Error);
        Assert.Equal("CUSTOMER_NOT_FOUND", error.ErrorCode);
        Assert.True(error.TraceId is { Length: > 0 });
        Assert.Empty(error.Violations);
    }

    [Fact]
    public async Task Unknown_exception_is_safe_and_does_not_expose_details()
    {
        using var server = CreateServer(app => app.Run(_ => throw new InvalidOperationException("secret connection string")));
        var response = await server.CreateClient().GetAsync("/");
        var raw = await response.Content.ReadAsStringAsync();
        var error = JsonSerializer.Deserialize<ApiErrorResponse>(raw, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("An unexpected error occurred", error!.Message);
        Assert.DoesNotContain("secret connection string", raw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Disabled_options_do_not_handle_exceptions()
    {
        using var server = CreateServer(app => app.Run(_ => throw new InvalidOperationException("unhandled")), options => options.Enabled = false);
        await Assert.ThrowsAsync<InvalidOperationException>(() => server.CreateClient().GetAsync("/"));
    }

    [Fact]
    public async Task Aspire_style_registration_is_compatible()
    {
        using var server = CreateServer(app => app.Run(_ => throw new ResourceNotFoundException("Missing")));
        var response = await server.CreateClient().GetAsync("/");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static TestServer CreateServer(Action<IApplicationBuilder> configure, Action<ExceptionHandlingOptions>? configureOptions = null)
    {
        var builder = new WebHostBuilder()
            .ConfigureServices(services => services.AddGenericExceptionHandling(configureOptions))
            .Configure(app =>
            {
                app.UseGenericExceptionHandling();
                configure(app);
            });
        return new TestServer(builder);
    }
}
