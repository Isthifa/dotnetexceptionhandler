namespace Isthifa.AspNetCore.ExceptionHandling.Models;

/// <summary>Represents the stable error document returned by the middleware.</summary>
public sealed record ApiErrorResponse
{
    /// <summary>Gets the UTC time at which the error was produced.</summary>
    public required DateTimeOffset Timestamp { get; init; }
    /// <summary>Gets the HTTP status code.</summary>
    public required int Status { get; init; }
    /// <summary>Gets the standard HTTP reason phrase.</summary>
    public required string Error { get; init; }
    /// <summary>Gets a client-safe error message.</summary>
    public required string Message { get; init; }
    /// <summary>Gets an optional stable application error code.</summary>
    public string? ErrorCode { get; init; }
    /// <summary>Gets the request or distributed tracing identifier, when enabled.</summary>
    public string? TraceId { get; init; }
    /// <summary>Gets field-level validation errors.</summary>
    public IReadOnlyList<FieldViolation> Violations { get; init; } = [];
}
