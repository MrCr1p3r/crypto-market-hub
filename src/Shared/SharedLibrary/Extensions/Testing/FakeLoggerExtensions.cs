using FluentAssertions.Execution;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace SharedLibrary.Extensions.Testing;

/// <summary>
/// Provides extension methods for testing logger implementations.
/// </summary>
public static class FakeLoggerExtensions
{
    /// <summary>
    /// Verifies that a log entry with the specified log level and containing the specified message was recorded.
    /// </summary>
    /// <typeparam name="T">The type of the logger.</typeparam>
    /// <param name="fakeLogger">The fake logger to verify.</param>
    /// <param name="logLevel">The expected log level.</param>
    /// <param name="message">The message substring expected to be in the log entry.</param>
    /// <exception cref="AssertionFailedException">Thrown when no matching log entry is found.</exception>
    public static void VerifyWasCalled<T>(
        this FakeLogger<T> fakeLogger,
        LogLevel logLevel,
        string message
    )
    {
        var hasLogRecord = fakeLogger
            .Collector.GetSnapshot()
            .Any(log =>
                log.Level == logLevel
                && log.Message.Contains(message, StringComparison.OrdinalIgnoreCase)
            );

        if (hasLogRecord)
        {
            return;
        }

        var exceptionMessage =
            $"Expected log entry with level [{logLevel}] and message containing '{message}' not found."
            + Environment.NewLine
            + $"Log entries found:"
            + Environment.NewLine
            + string.Join(Environment.NewLine, fakeLogger.Collector.GetSnapshot().Select(l => l));

        throw new AssertionFailedException(exceptionMessage);
    }
}
