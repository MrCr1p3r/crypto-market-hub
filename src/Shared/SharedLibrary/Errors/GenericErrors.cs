using FluentResults;

namespace SharedLibrary.Errors;

/// <summary>
/// Contains custom error types for domain operations
/// </summary>
public static class GenericErrors
{
    /// <summary>
    /// Bad request error (invalid input)
    /// </summary>
    public class BadRequestError : Error
    {
        public BadRequestError(string message)
            : base(message)
        {
            Metadata.Add("ErrorCode", "BadRequest");
        }
    }

    /// <summary>
    /// Unauthorized error (not authenticated)
    /// </summary>
    public class UnauthorizedError : Error
    {
        public UnauthorizedError(string message)
            : base(message)
        {
            Metadata.Add("ErrorCode", "Unauthorized");
        }
    }

    /// <summary>
    /// Forbidden error (not authorized)
    /// </summary>
    public class ForbiddenError : Error
    {
        public ForbiddenError(string message)
            : base(message)
        {
            Metadata.Add("ErrorCode", "Forbidden");
        }
    }

    /// <summary>
    /// Not found error (resource doesn't exist)
    /// </summary>
    public class NotFoundError : Error
    {
        public NotFoundError(string message)
            : base(message)
        {
            Metadata.Add("ErrorCode", "NotFound");
        }
    }

    /// <summary>
    /// Conflict error (resource already exists or state conflict)
    /// </summary>
    public class ConflictError : Error
    {
        public ConflictError(string message)
            : base(message)
        {
            Metadata.Add("ErrorCode", "Conflict");
        }
    }

    /// <summary>
    /// Too many requests error (rate limit exceeded)
    /// </summary>
    public class TooManyRequestsError : Error
    {
        public TooManyRequestsError(string message)
            : base(message)
        {
            Metadata.Add("ErrorCode", "TooManyRequests");
        }
    }

    /// <summary>
    /// Internal error (internal server error)
    /// </summary>
    public class InternalError : Error
    {
        public InternalError(string message)
            : base(message)
        {
            Metadata.Add("ErrorCode", "InternalError");
        }
    }

    /// <summary>
    /// Bad gateway error (bad gateway)
    /// </summary>
    public class GatewayError : Error
    {
        public GatewayError(string message)
            : base(message)
        {
            Metadata.Add("ErrorCode", "BadGateway");
        }
    }

    /// <summary>
    /// Unavailable error (unavailable)
    /// </summary>
    public class UnavailableError : Error
    {
        public UnavailableError(string message)
            : base(message)
        {
            Metadata.Add("ErrorCode", "Unavailable");
        }
    }

    /// <summary>
    /// Timeout error (timeout)
    /// </summary>
    public class TimeoutError : Error
    {
        public TimeoutError(string message)
            : base(message)
        {
            Metadata.Add("ErrorCode", "Timeout");
        }
    }
}
