namespace Isthifa.AspNetCore.ExceptionHandling.Models;

/// <summary>Describes a validation error for one request field.</summary>
public sealed record FieldViolation
{
    /// <summary>Initializes a field violation.</summary>
    /// <param name="field">The field or JSON path that failed validation.</param>
    /// <param name="message">A safe validation message.</param>
    /// <param name="errorCode">An optional validator-specific error code.</param>
    public FieldViolation(string field, string message, string? errorCode = null)
    {
        Field = field;
        Message = message;
        ErrorCode = errorCode;
    }

    /// <summary>Gets the field or JSON path.</summary>
    public string Field { get; init; }
    /// <summary>Gets the validation message.</summary>
    public string Message { get; init; }
    /// <summary>Gets the optional validator error code.</summary>
    public string? ErrorCode { get; init; }
}
