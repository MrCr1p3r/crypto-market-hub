using GUI_Crypto.Hubs;
using GUI_Crypto.ServiceModels.Messaging;
using Microsoft.AspNetCore.SignalR;
using SharedLibrary.Models.Messaging;

namespace GUI_Crypto.Services.Messaging;

/// <summary>
/// Handles kline data update messages from the scheduler service.
/// </summary>
public class KlineDataMessageHandler(
    IHubContext<CryptoHub, ICryptoHubClient> hubContext,
    ILogger<KlineDataMessageHandler> logger
) : BaseMessageHandler<IEnumerable<KlineData>>(logger)
{
    private readonly IHubContext<CryptoHub, ICryptoHubClient> _hubContext = hubContext;

    protected override async Task HandleSuccess(
        JobCompletedMessage message,
        IEnumerable<KlineData> data,
        CancellationToken cancellationToken
    )
    {
        await _hubContext.Clients.Group("KlineDataSubscribers").ReceiveKlineDataUpdate(data);
    }
}
