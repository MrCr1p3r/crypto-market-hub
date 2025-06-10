using GUI_Crypto.Hubs;
using GUI_Crypto.ServiceModels.Messaging;
using Microsoft.AspNetCore.SignalR;
using SharedLibrary.Models.Messaging;

namespace GUI_Crypto.Services.Messaging;

/// <summary>
/// Handles market data update messages from the scheduler service.
/// </summary>
public class MarketDataMessageHandler(
    IHubContext<CryptoHub, ICryptoHubClient> hubContext,
    ILogger<MarketDataMessageHandler> logger
) : BaseMessageHandler<IEnumerable<CoinMarketData>>(logger)
{
    private readonly IHubContext<CryptoHub, ICryptoHubClient> _hubContext = hubContext;

    protected override async Task HandleSuccess(
        JobCompletedMessage message,
        IEnumerable<CoinMarketData> data,
        CancellationToken cancellationToken
    )
    {
        await _hubContext.Clients.Group("MarketDataSubscribers").ReceiveMarketDataUpdate(data);
    }
}
