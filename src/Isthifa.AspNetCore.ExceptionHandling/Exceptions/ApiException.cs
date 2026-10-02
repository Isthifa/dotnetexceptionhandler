namespace Isthifa.AspNetCore.ExceptionHandling.Exceptions;

/// <summary>Represents an API error that has a known HTTP status code.</summary>
public class ApiException : Exception
{
    /// <summary>Initializes a new API exception.</summary>
    /// <param name="statusCode">The HTTP status code to return.</param>
    /// <param name="message">A safe message for the API client.</param>
    /// <param name="errorCode">An optional stable application error code.</param>
    /// <param name="innerException">The underlying exception, which is never returned to clients.</param>
    public ApiException(int statusCode, string message, string? errorCode = null, Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }

    /// <summary>Gets the HTTP status code.</summary>
    public int StatusCode { get; }

    /// <summary>Gets the optional stable error code.</summary>
    public string? ErrorCode { get; }
}
