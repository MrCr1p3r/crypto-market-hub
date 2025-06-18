using Coravel.Invocable;
using SharedLibrary.Constants;
using SharedLibrary.Messaging;
using SharedLibrary.Models.Messaging;
using SVC_Scheduler.MicroserviceClients.SvcExternal;

namespace SVC_Scheduler.Jobs.CacheWarmup;

/// <summary>
/// Scheduled job that warms up the spot coins cache by calling SVC_External.
/// </summary>
public class SpotCoinsCacheWarmupJob(
    ISvcExternalClient svcExternalClient,
    IMessagePublisher messagePublisher,
    ILogger<SpotCoinsCacheWarmupJob> logger
) : IInvocable
{
    private readonly ISvcExternalClient _svcExternalClient = svcExternalClient;
    private readonly IMessagePublisher _messagePublisher = messagePublisher;
    private readonly ILogger<SpotCoinsCacheWarmupJob> _logger = logger;

    private static readonly object _lock = new();
    private static bool _firstWarmupCompleted;

    public async Task Invoke()
    {
        _logger.LogJobStarted();

        var result = await _svcExternalClient.GetAllSpotCoins();

        if (result.IsSuccess)
        {
            var coinCount = result.Value.Count();
            _logger.LogSpotCoinsRetrieved(coinCount);

            // Check if this is the first successful warmup
            var isFirstWarmup = false;
            lock (_lock)
            {
                if (!_firstWarmupCompleted)
                {
                    _firstWarmupCompleted = true;
                    isFirstWarmup = true;
                }
            }

            // Send one-time notification for first successful warmup
            if (isFirstWarmup)
            {
                await PublishCacheWarmupCompletedMessage(coinCount);
                _logger.LogFirstCacheWarmupCompleted(coinCount);
            }
        }
        else
        {
            _logger.LogSpotCoinsRetrievalFailed(result.Errors[0]);
        }

        _logger.LogJobCompleted();
    }

    /// <summary>
    /// Publishes a one-time message indicating cache warmup has completed.
    /// </summary>
    /// <param name="coinCount">Number of coins retrieved during warmup.</param>
    private async Task PublishCacheWarmupCompletedMessage(int coinCount)
    {
        var message = new JobCompletedMessage
        {
            JobName = JobConstants.Names.SpotCoinsCacheWarmup,
            JobType = JobConstants.Types.DataSync,
            CompletedAt = DateTime.UtcNow,
            Success = true,
            Data = new { CoinCount = coinCount, IsFirstWarmup = true },
            Source = JobConstants.Sources.Scheduler,
        };

        await _messagePublisher.PublishAsync(
            JobConstants.RoutingKeys.CacheWarmupCompleted,
            message
        );

        _logger.LogCacheWarmupMessagePublished();
    }
}
