using FluentResults;
using SharedLibrary.Constants;
using SharedLibrary.Messaging;
using SVC_Scheduler.Jobs.UpdateJobs.Base;
using SVC_Scheduler.MicroserviceClients.SvcBridge;
using SVC_Scheduler.MicroserviceClients.SvcBridge.Responses.KlineData;

namespace SVC_Scheduler.Jobs.UpdateJobs;

/// <summary>
/// Scheduled job that updates kline data for all trading pairs.
/// </summary>
public class KlineDataUpdateJob(
    ISvcBridgeClient svcBridgeClient,
    IMessagePublisher messagePublisher,
    ILogger<KlineDataUpdateJob> logger
) : BaseScheduledUpdateJob<IEnumerable<KlineDataResponse>>(messagePublisher, logger)
{
    private readonly ISvcBridgeClient _svcBridgeClient = svcBridgeClient;

    protected override string JobName => JobConstants.Names.KlineDataUpdate;

    protected override string RoutingKey => JobConstants.RoutingKeys.KlineDataUpdated;

    protected override async Task<Result<IEnumerable<KlineDataResponse>>> ExecuteJobAsync() =>
        await _svcBridgeClient.UpdateKlineData();
}
