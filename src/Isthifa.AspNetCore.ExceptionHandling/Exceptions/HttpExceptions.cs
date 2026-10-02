namespace Isthifa.AspNetCore.ExceptionHandling.Exceptions;

/// <summary>Represents a 400 Bad Request error.</summary>
public sealed class BadRequestException(string message, string? errorCode = null, Exception? innerException = null)
    : ApiException(400, message, errorCode, innerException);

/// <summary>Represents a 401 Unauthorized error.</summary>
public sealed class UnauthorizedException(string message = "Authentication is required", string? errorCode = null, Exception? innerException = null)
    : ApiException(401, message, errorCode, innerException);

/// <summary>Represents a 403 Forbidden error.</summary>
public sealed class ForbiddenException(string message = "You do not have permission to perform this action", string? errorCode = null, Exception? innerException = null)
    : ApiException(403, message, errorCode, innerException);

/// <summary>Represents a 404 Not Found error.</summary>
public sealed class ResourceNotFoundException(string message, string? errorCode = null, Exception? innerException = null)
    : ApiException(404, message, errorCode, innerException);

/// <summary>Represents a 409 Conflict error.</summary>
public sealed class ConflictException(string message, string? errorCode = null, Exception? innerException = null)
    : ApiException(409, message, errorCode, innerException);
