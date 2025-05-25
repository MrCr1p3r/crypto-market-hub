namespace SharedLibrary.Exceptions;

/// <summary>
/// Represents an exception thrown when an error response or object, expected to be in
/// RFC 9457 Problem Details format, is either not in that format or is malformed.
/// This exception indicates a failure to process or interpret an error due to
/// non-compliance with the expected Problem Details structure.
/// </summary>
/// <param name="message">The message that describes the error, typically detailing the parsing or validation failure.</param>
/// <param name="innerException">
/// The exception that is the cause of the current exception (e.g., a deserialization exception),
/// or a null reference if no inner exception is specified.
/// </param>
public class ProblemDetailsException(string message, Exception innerException)
    : Exception(message, innerException) { }
