using FluentResults;

namespace SVC_Scheduler.Jobs.CacheWarmup;

/// <summary>
/// Source-generated logging methods for SpotCoinsCacheWarmupJob.
/// </summary>
internal static partial class SpotCoinsCacheWarmupJobLogging
{
    [LoggerMessage(
        EventId = 5001,
        Level = LogLevel.Information,
        Message = "SpotCoinsCacheWarmupJob started"
    )]
    internal static partial void LogJobStarted(this ILogger logger);

    [LoggerMessage(
        EventId = 5002,
        Level = LogLevel.Information,
        Message = "Successfully retrieved {CoinCount} spot coins for cache warmup"
    )]
    internal static partial void LogSpotCoinsRetrieved(this ILogger logger, int coinCount);

    [LoggerMessage(
        EventId = 5004,
        Level = LogLevel.Warning,
        Message = "Failed to retrieve spot coins for cache warmup: {@Error}"
    )]
    internal static partial void LogSpotCoinsRetrievalFailed(this ILogger logger, IError error);

    [LoggerMessage(
        EventId = 5006,
        Level = LogLevel.Information,
        Message = "SpotCoinsCacheWarmupJob completed"
    )]
    internal static partial void LogJobCompleted(this ILogger logger);
}
