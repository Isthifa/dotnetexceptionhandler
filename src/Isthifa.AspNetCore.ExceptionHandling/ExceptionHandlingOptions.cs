namespace Isthifa.AspNetCore.ExceptionHandling;

/// <summary>Configures generic exception handling.</summary>
public sealed class ExceptionHandlingOptions
{
    /// <summary>Gets or sets whether exception handling is active. Defaults to <see langword="true"/>.</summary>
    public bool Enabled { get; set; } = true;
    /// <summary>Gets or sets whether safe details for known <c>ApiException</c>s are included. Defaults to <see langword="false"/>.</summary>
    /// <remarks>Stack traces, inner exception details, and unknown-exception details are never returned.</remarks>
    public bool IncludeExceptionDetails { get; set; }
    /// <summary>Gets or sets whether a request or activity trace identifier is included. Defaults to <see langword="true"/>.</summary>
    public bool EnableTraceId { get; set; } = true;
}
