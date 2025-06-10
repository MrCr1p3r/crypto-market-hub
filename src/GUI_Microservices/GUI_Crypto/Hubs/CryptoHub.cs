using Microsoft.AspNetCore.SignalR;

namespace GUI_Crypto.Hubs;

/// <summary>
/// SignalR hub for broadcasting real-time crypto data updates to connected clients.
/// </summary>
public class CryptoHub : Hub<ICryptoHubClient>
{
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
