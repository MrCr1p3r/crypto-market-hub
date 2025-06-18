using GUI_Crypto.ServiceModels.Messaging;

namespace GUI_Crypto.Hubs;

/// <summary>
/// Interface for crypto hub client methods.
/// </summary>
public interface ICryptoHubClient
{
    /// <summary>
    /// Receives market data updates.
    /// </summary>
    /// <param name="marketData">The updated market data.</param>
    /// <returns>A task that receives market data updates.</returns>
    Task ReceiveMarketDataUpdate(IEnumerable<CoinMarketData> marketData);

    /// <summary>
    /// Receives kline data updates.
    /// </summary>
    /// <param name="klineData">The updated kline data.</param>
    /// <returns>A task that receives kline data updates.</returns>
    Task ReceiveKlineDataUpdate(IEnumerable<KlineData> klineData);

    /// <summary>
    /// Notifies clients that the initial cache warmup has completed.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ReceiveCacheWarmupCompleted();
}
