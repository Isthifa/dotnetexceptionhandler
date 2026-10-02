using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Isthifa.AspNetCore.ExceptionHandling.Exceptions;
using Isthifa.AspNetCore.ExceptionHandling.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Isthifa.AspNetCore.ExceptionHandling.Middleware;

/// <summary>Catches unhandled exceptions and writes a consistent, client-safe API error response.</summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IOptions<ExceptionHandlingOptions> _options;

    /// <summary>Initializes the middleware.</summary>
    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IOptions<ExceptionHandlingOptions> options)
    {
        _next = next;
        _logger = logger;
        _options = options;
    }

    /// <summary>Invokes the exception-handling pipeline.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.Value.Enabled)
        {
            await _next(context);
            return;
        }

        try
        {
            await _next(context);
        }
        catch (Exception exception) when (!context.Response.HasStarted)
        {
            await WriteResponseAsync(context, exception);
        }
    }

    private async Task WriteResponseAsync(HttpContext context, Exception exception)
    {
        var mapped = Map(exception);
        if (mapped.IsUnexpected)
        {
            _logger.LogError(exception, "An unhandled exception occurred while processing {Method} {Path}. TraceId: {TraceId}", context.Request.Method, context.Request.Path, context.TraceIdentifier);
        }

        context.Response.Clear();
        context.Response.StatusCode = mapped.StatusCode;
        context.Response.ContentType = "application/problem+json";
        var response = new ApiErrorResponse
        {
            Timestamp = DateTimeOffset.UtcNow,
            Status = mapped.StatusCode,
            Error = ReasonPhrases.GetReasonPhrase(mapped.StatusCode),
            Message = mapped.Message,
            ErrorCode = mapped.ErrorCode,
            TraceId = _options.Value.EnableTraceId ? Activity.Current?.Id ?? context.TraceIdentifier : null,
            Violations = mapped.Violations,
        };

        await context.Response.WriteAsJsonAsync(response);
    }

    private static MappedException Map(Exception exception)
    {
        if (exception is ValidationException validation)
            return new(validation.StatusCode, validation.Message, validation.ErrorCode, validation.Violations, false);
        if (exception is ApiException api)
            return new(api.StatusCode, api.Message, api.ErrorCode, [], false);
        if (exception is BadHttpRequestException or JsonException)
            return new(StatusCodes.Status400BadRequest, "The request is invalid", null, [], false);
        if (IsFluentValidationException(exception, out var violations))
            return new(StatusCodes.Status400BadRequest, "One or more validation errors occurred", null, violations, false);
        if (IsConflict(exception))
            return new(StatusCodes.Status409Conflict, "The request conflicts with existing data", null, [], false);

        return new(StatusCodes.Status500InternalServerError, "An unexpected error occurred", null, [], true);
    }

    private static bool IsFluentValidationException(Exception exception, out IReadOnlyList<FieldViolation> violations)
    {
        violations = [];
        if (!string.Equals(exception.GetType().FullName, "FluentValidation.ValidationException", StringComparison.Ordinal))
            return false;

        var errors = exception.GetType().GetProperty("Errors")?.GetValue(exception) as System.Collections.IEnumerable;
        if (errors is null) return true;
        violations = errors.Cast<object>().Select(error => new FieldViolation(
            error.GetType().GetProperty("PropertyName")?.GetValue(error)?.ToString() ?? string.Empty,
            error.GetType().GetProperty("ErrorMessage")?.GetValue(error)?.ToString() ?? "Validation failed",
            error.GetType().GetProperty("ErrorCode")?.GetValue(error)?.ToString())).ToArray();
        return true;
    }

    private static bool IsConflict(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            var typeName = current.GetType().FullName ?? current.GetType().Name;
            if (typeName.Contains("DbUpdateException", StringComparison.Ordinal) ||
                typeName.Contains("Duplicate", StringComparison.OrdinalIgnoreCase) ||
                typeName.Contains("UniqueConstraint", StringComparison.OrdinalIgnoreCase) ||
                typeName.Contains("Conflict", StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private sealed record MappedException(int StatusCode, string Message, string? ErrorCode, IReadOnlyList<FieldViolation> Violations, bool IsUnexpected);
}
