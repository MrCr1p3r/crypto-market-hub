using GUI_Crypto.Hubs;
using GUI_Crypto.Infrastructure.Caching;
using Microsoft.AspNetCore.SignalR;

namespace GUI_Crypto.Services.Messaging;

/// <summary>
/// Handles cache warmup completion messages and notifies clients via SignalR.
/// </summary>
public partial class CacheWarmupMessageHandler(
    IHubContext<CryptoHub, ICryptoHubClient> hubContext,
    ICacheWarmupStateService cacheWarmupStateService,
    ILogger<CacheWarmupMessageHandler> logger
)
{
    private readonly IHubContext<CryptoHub, ICryptoHubClient> _hubContext = hubContext;
    private readonly ICacheWarmupStateService _cacheWarmupStateService = cacheWarmupStateService;
    private readonly ILogger<CacheWarmupMessageHandler> _logger = logger;

    /// <summary>
    /// Handles cache warmup completion messages.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task HandleAsync()
    {
        _cacheWarmupStateService.MarkAsWarmedUp();

        Logging.LogCacheWarmupCompleted(_logger);

        await _hubContext.Clients.All.ReceiveCacheWarmupCompleted();
    }

    /// <summary>
    /// Source-generated logging methods for CacheWarmupMessageHandler.
    /// </summary>
    private static partial class Logging
    {
        [LoggerMessage(
            EventId = 7002,
            Level = LogLevel.Information,
            Message = "Cache warmup completed successfully - notifying all connected clients"
        )]
        public static partial void LogCacheWarmupCompleted(ILogger logger);
    }
}
