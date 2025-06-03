using FluentResults;
using SharedLibrary.Constants;
using SharedLibrary.Messaging;
using SVC_Scheduler.Jobs.UpdateJobs.Base;
using SVC_Scheduler.MicroserviceClients.SvcBridge;
using SVC_Scheduler.MicroserviceClients.SvcBridge.Responses;

namespace SVC_Scheduler.Jobs.UpdateJobs;

/// <summary>
/// Scheduled job that updates market data for all coins.
/// </summary>
public class MarketDataUpdateJob(
    ISvcBridgeClient svcBridgeClient,
    IMessagePublisher messagePublisher,
    ILogger<MarketDataUpdateJob> logger
) : BaseScheduledUpdateJob<IEnumerable<CoinMarketData>>(messagePublisher, logger)
{
    private readonly ISvcBridgeClient _svcBridgeClient = svcBridgeClient;

    protected override string JobName => JobConstants.Names.MarketDataUpdate;

    protected override string RoutingKey => JobConstants.RoutingKeys.MarketDataUpdated;

    protected override async Task<Result<IEnumerable<CoinMarketData>>> ExecuteJobAsync() =>
        await _svcBridgeClient.UpdateCoinsMarketData();
}
