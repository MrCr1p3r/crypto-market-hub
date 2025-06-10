namespace GUI_Crypto.Services.Messaging;

/// <summary>
/// Source-generated logging methods for BaseMessageHandler.
/// </summary>
internal static partial class BaseMessageHandlerLogging
{
    [LoggerMessage(
        EventId = 4001,
        Level = LogLevel.Warning,
        Message = "Job {JobName} failed - {ErrorMessage}"
    )]
    internal static partial void LogJobFailed(
        this ILogger logger,
        string jobName,
        string? errorMessage
    );

    [LoggerMessage(
        EventId = 4002,
        Level = LogLevel.Debug,
        Message = "Job {JobName} completed successfully without data"
    )]
    internal static partial void LogSuccessWithoutData(this ILogger logger, string jobName);
}
