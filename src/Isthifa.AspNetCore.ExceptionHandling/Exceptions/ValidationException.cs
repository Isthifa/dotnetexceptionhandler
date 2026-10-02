using Isthifa.AspNetCore.ExceptionHandling.Models;

namespace Isthifa.AspNetCore.ExceptionHandling.Exceptions;

/// <summary>Represents a 400 validation error with optional field-level violations.</summary>
public sealed class ValidationException : ApiException
{
    /// <summary>Initializes a validation exception.</summary>
    /// <param name="message">A safe validation error message.</param>
    /// <param name="violations">Field-level validation violations.</param>
    /// <param name="errorCode">An optional stable error code.</param>
    /// <param name="innerException">The underlying exception.</param>
    public ValidationException(string message, IEnumerable<FieldViolation>? violations = null, string? errorCode = null, Exception? innerException = null)
        : base(400, message, errorCode, innerException) => Violations = violations?.ToArray() ?? [];

    /// <summary>Gets field-level validation violations.</summary>
    public IReadOnlyList<FieldViolation> Violations { get; }
}
