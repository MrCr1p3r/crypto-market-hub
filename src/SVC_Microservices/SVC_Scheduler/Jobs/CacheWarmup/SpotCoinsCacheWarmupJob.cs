using Coravel.Invocable;
using SVC_Scheduler.MicroserviceClients.SvcExternal;

namespace SVC_Scheduler.Jobs.CacheWarmup;

/// <summary>
/// Scheduled job that warms up the spot coins cache by calling SVC_External.
/// </summary>
public class SpotCoinsCacheWarmupJob(
    ISvcExternalClient svcExternalClient,
    ILogger<SpotCoinsCacheWarmupJob> logger
) : IInvocable
{
    private readonly ISvcExternalClient _svcExternalClient = svcExternalClient;
    private readonly ILogger<SpotCoinsCacheWarmupJob> _logger = logger;

    public async Task Invoke()
    {
        _logger.LogJobStarted();

        var result = await _svcExternalClient.GetAllSpotCoins();

        if (result.IsSuccess)
        {
            var coinCount = result.Value.Count();
            _logger.LogSpotCoinsRetrieved(coinCount);
        }
        else
        {
            _logger.LogSpotCoinsRetrievalFailed(result.Errors[0]);
        }

        _logger.LogJobCompleted();
    }
}
