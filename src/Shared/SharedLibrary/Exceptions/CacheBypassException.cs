using FluentResults;

namespace SharedLibrary.Exceptions;

/// <summary>
/// Represents an exception that is thrown to indicate that a caching mechanism should be bypassed,
/// and a pre-existing failed result should be returned directly.
/// </summary>
/// <typeparam name="T">The type of the value in the Result payload.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="CacheBypassException{T}"/> class
/// with the specified payload.
/// </remarks>
/// <param name="result">The result payload that should be forwarded.</param>
public class CacheBypassException<T>(Result<T> result)
    : Exception("Bypassing cache—forwarding failed result")
{
    /// <summary>
    /// Gets the payload containing the failed result that led to this exception.
    /// </summary>
    public Result<T> Result { get; } = result;
}
