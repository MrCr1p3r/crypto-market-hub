namespace SVC_Scheduler.Jobs.UpdateJobs.Base;

/// <summary>
/// Source-generated logging methods for BaseScheduledJob.
/// </summary>
internal static partial class BaseScheduledJobLogging
{
    // Job execution lifecycle logs
    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Starting {JobName} job"
    )]
    internal static partial void LogJobStarting(this ILogger logger, string jobName);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Information,
        Message = "{JobName} completed successfully in {ElapsedMs}ms"
    )]
    internal static partial void LogJobCompletedSuccessfully(
        this ILogger logger,
        string jobName,
        long elapsedMs
    );

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Error,
        Message = "{JobName} failed after {ElapsedMs}ms: {Error}"
    )]
    internal static partial void LogJobFailed(
        this ILogger logger,
        string jobName,
        long elapsedMs,
        string error
    );

    [LoggerMessage(
        EventId = 1004,
        Level = LogLevel.Error,
        Message = "{JobName} threw exception after {ElapsedMs}ms"
    )]
    internal static partial void LogJobException(
        this ILogger logger,
        Exception exception,
        string jobName,
        long elapsedMs
    );

    // Message publishing logs
    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Debug,
        Message = "Published success message for {JobName} to {RoutingKey}"
    )]
    internal static partial void LogSuccessMessagePublished(
        this ILogger logger,
        string jobName,
        string routingKey
    );

    [LoggerMessage(
        EventId = 2002,
        Level = LogLevel.Debug,
        Message = "Published error message for {JobName} to {RoutingKey}"
    )]
    internal static partial void LogErrorMessagePublished(
        this ILogger logger,
        string jobName,
        string routingKey
    );

    [LoggerMessage(
        EventId = 2003,
        Level = LogLevel.Debug,
        Message = "Published exception message for {JobName} to {RoutingKey}"
    )]
    internal static partial void LogExceptionMessagePublished(
        this ILogger logger,
        string jobName,
        string routingKey
    );

    // Additional performance and debugging logs
    [LoggerMessage(
        EventId = 3001,
        Level = LogLevel.Debug,
        Message = "Job {JobName} ({JobType}) execution started at {StartTime}"
    )]
    internal static partial void LogJobExecutionStarted(
        this ILogger logger,
        string jobName,
        string jobType,
        DateTime startTime
    );

    [LoggerMessage(
        EventId = 3002,
        Level = LogLevel.Debug,
        Message = "Job {JobName} result processing completed - Success: {Success}, Data available: {HasData}"
    )]
    internal static partial void LogJobResultProcessed(
        this ILogger logger,
        string jobName,
        bool success,
        bool hasData
    );

    [LoggerMessage(
        EventId = 3003,
        Level = LogLevel.Warning,
        Message = "Job {JobName} completed with warnings - Duration: {ElapsedMs}ms, Warning count: {WarningCount}"
    )]
    internal static partial void LogJobCompletedWithWarnings(
        this ILogger logger,
        string jobName,
        long elapsedMs,
        int warningCount
    );
}
