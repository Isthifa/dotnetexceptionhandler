using System.Diagnostics;
using Isthifa.AspNetCore.ExceptionHandling.Models;
using Isthifa.AspNetCore.ExceptionHandling.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Isthifa.AspNetCore.ExceptionHandling.Extensions;

/// <summary>Registers generic exception handling services and middleware.</summary>
public static class ExceptionHandlingExtensions
{
    /// <summary>Registers generic exception handling and consistent MVC invalid-model-state responses.</summary>
    /// <param name="services">The application service collection.</param>
    /// <param name="configure">An optional options configuration delegate.</param>
    /// <returns>The original service collection.</returns>
    public static IServiceCollection AddGenericExceptionHandling(this IServiceCollection services, Action<ExceptionHandlingOptions>? configure = null)
    {
        var options = services.AddOptions<ExceptionHandlingOptions>();
        if (configure is not null) options.Configure(configure);

        services.Configure<ApiBehaviorOptions>(api => api.InvalidModelStateResponseFactory = context =>
        {
            var violations = context.ModelState
                .Where(pair => pair.Value is { Errors.Count: > 0 })
                .SelectMany(pair => pair.Value!.Errors.Select(error => new FieldViolation(pair.Key, string.IsNullOrWhiteSpace(error.ErrorMessage) ? "The supplied value is invalid" : error.ErrorMessage)))
                .ToArray();
            var response = new ApiErrorResponse
            {
                Timestamp = DateTimeOffset.UtcNow,
                Status = StatusCodes.Status400BadRequest,
                Error = "Bad Request",
                Message = "One or more validation errors occurred",
                TraceId = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier,
                Violations = violations,
            };
            return new BadRequestObjectResult(response) { ContentTypes = { "application/problem+json" } };
        });
        return services;
    }

    /// <summary>Adds generic exception handling early in the application pipeline.</summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The original application builder.</returns>
    public static IApplicationBuilder UseGenericExceptionHandling(this IApplicationBuilder app) => app.UseMiddleware<ExceptionHandlingMiddleware>();
}
