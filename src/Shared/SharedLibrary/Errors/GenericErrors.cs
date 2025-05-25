using FluentResults;

namespace SharedLibrary.Errors;

/// <summary>
/// Contains custom error types for domain operations.
/// </summary>
public static class GenericErrors
{
    /// <summary>
    /// Bad request error (invalid input).
    /// </summary>
    public class BadRequestError : Error
    {
        public BadRequestError(
            string message,
            Dictionary<string, object>? metadata = null,
            IEnumerable<IError>? reasons = null
        )
            : base(message)
        {
            this.AddMetadata(metadata);
            this.AddReasons(reasons);
        }
    }

    /// <summary>
    /// Unauthorized error (not authenticated).
    /// </summary>
    public class UnauthorizedError : Error
    {
        public UnauthorizedError(
            string message,
            Dictionary<string, object>? metadata = null,
            IEnumerable<IError>? reasons = null
        )
            : base(message)
        {
            this.AddMetadata(metadata);
            this.AddReasons(reasons);
        }
    }

    /// <summary>
    /// Forbidden error (not authorized).
    /// </summary>
    public class ForbiddenError : Error
    {
        public ForbiddenError(
            string message,
            Dictionary<string, object>? metadata = null,
            IEnumerable<IError>? reasons = null
        )
            : base(message)
        {
            this.AddMetadata(metadata);
            this.AddReasons(reasons);
        }
    }

    /// <summary>
    /// Not found error (resource doesn't exist).
    /// </summary>
    public class NotFoundError : Error
    {
        public NotFoundError(
            string message,
            Dictionary<string, object>? metadata = null,
            IEnumerable<IError>? reasons = null
        )
            : base(message)
        {
            this.AddMetadata(metadata);
            this.AddReasons(reasons);
        }
    }

    /// <summary>
    /// Conflict error (resource already exists or state conflict).
    /// </summary>
    public class ConflictError : Error
    {
        public ConflictError(
            string message,
            Dictionary<string, object>? metadata = null,
            IEnumerable<IError>? reasons = null
        )
            : base(message)
        {
            this.AddMetadata(metadata);
            this.AddReasons(reasons);
        }
    }

    /// <summary>
    /// Too many requests error (rate limit exceeded).
    /// </summary>
    public class TooManyRequestsError : Error
    {
        public TooManyRequestsError(
            string message,
            Dictionary<string, object>? metadata = null,
            IEnumerable<IError>? reasons = null
        )
            : base(message)
        {
            this.AddMetadata(metadata);
            this.AddReasons(reasons);
        }
    }

    /// <summary>
    /// Internal error (internal server error).
    /// </summary>
    public class InternalError : Error
    {
        public InternalError(
            string message,
            Dictionary<string, object>? metadata = null,
            IEnumerable<IError>? reasons = null
        )
            : base(message)
        {
            this.AddMetadata(metadata);
            this.AddReasons(reasons);
        }
    }

    /// <summary>
    /// Bad gateway error (bad gateway).
    /// </summary>
    public class GatewayError : Error
    {
        public GatewayError(
            string message,
            Dictionary<string, object>? metadata = null,
            IEnumerable<IError>? reasons = null
        )
            : base(message)
        {
            this.AddMetadata(metadata);
            this.AddReasons(reasons);
        }
    }

    /// <summary>
    /// Unavailable error (unavailable).
    /// </summary>
    public class UnavailableError : Error
    {
        public UnavailableError(
            string message,
            Dictionary<string, object>? metadata = null,
            IEnumerable<IError>? reasons = null
        )
            : base(message)
        {
            this.AddMetadata(metadata);
            this.AddReasons(reasons);
        }
    }

    /// <summary>
    /// Timeout error (timeout).
    /// </summary>
    public class TimeoutError : Error
    {
        public TimeoutError(
            string message,
            Dictionary<string, object>? metadata = null,
            IEnumerable<IError>? reasons = null
        )
            : base(message)
        {
            this.AddMetadata(metadata);
            this.AddReasons(reasons);
        }
    }

    private static void AddMetadata(this Error error, Dictionary<string, object>? metadata)
    {
        if (metadata is not null)
        {
            error.WithMetadata(metadata);
        }
    }

    private static void AddReasons(this Error error, IEnumerable<IError>? reasons)
    {
        if (reasons is not null)
        {
            error.CausedBy(reasons);
        }
    }
}
