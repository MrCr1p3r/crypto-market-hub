using FluentResults;
using SharedLibrary.Constants;
using SharedLibrary.Messaging;
using SVC_Scheduler.Jobs.UpdateJobs.Base;
using SVC_Scheduler.MicroserviceClients.SvcBridge;
using SVC_Scheduler.MicroserviceClients.SvcBridge.Responses.Coins;

namespace SVC_Scheduler.Jobs.UpdateJobs;

/// <summary>
/// Scheduled job that updates trading pairs for all coins.
/// </summary>
public class TradingPairsUpdateJob(
    ISvcBridgeClient svcBridgeClient,
    IMessagePublisher messagePublisher,
    ILogger<TradingPairsUpdateJob> logger
) : BaseScheduledUpdateJob<IEnumerable<Coin>>(messagePublisher, logger)
{
    private readonly ISvcBridgeClient _svcBridgeClient = svcBridgeClient;

    protected override string JobName => JobConstants.Names.TradingPairsUpdate;

    protected override string RoutingKey => JobConstants.RoutingKeys.TradingPairsUpdated;

    protected override async Task<Result<IEnumerable<Coin>>> ExecuteJobAsync() =>
        await _svcBridgeClient.UpdateTradingPairs();
}
