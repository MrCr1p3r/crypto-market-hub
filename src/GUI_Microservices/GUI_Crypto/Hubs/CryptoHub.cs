using GUI_Crypto.Infrastructure.Caching;
using Microsoft.AspNetCore.SignalR;

namespace GUI_Crypto.Hubs;

/// <summary>
/// SignalR hub for broadcasting real-time crypto data updates to connected clients.
/// </summary>
public class CryptoHub(ICacheWarmupStateService cacheWarmupStateService) : Hub<ICryptoHubClient>
{
    private readonly ICacheWarmupStateService _cacheWarmupStateService = cacheWarmupStateService;

    /// <summary>
    /// Called when a client connects to the hub.
    /// </summary>
    /// <returns>A task representing the asynchronous connection operation.</returns>
    public override async Task OnConnectedAsync()
    {
        if (_cacheWarmupStateService.IsWarmedUp)
        {
            await Clients.Caller.ReceiveCacheWarmupCompleted();
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects from the hub.
    /// </summary>
    /// <param name="exception">The exception that occurred during the disconnection.</param>
    /// <returns>A task that unsubscribes from overview updates on disconnect.</returns>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await UnsubscribeFromOverviewUpdates();
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Allows clients to subscribe to overview updates.
    /// </summary>
    /// <returns>A task that subscribes to overview updates.</returns>
    public async Task SubscribeToOverviewUpdates()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "MarketDataSubscribers");
        await Groups.AddToGroupAsync(Context.ConnectionId, "KlineDataSubscribers");
    }

    /// <summary>
    /// Allows clients to unsubscribe from overview updates.
    /// </summary>
    /// <returns>A task that unsubscribes from overview updates.</returns>
    public async Task UnsubscribeFromOverviewUpdates()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "MarketDataSubscribers");
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "KlineDataSubscribers");
    }
}
