using SVC_External.Models.Input;
using SVC_External.Models.Output;

namespace SVC_External.DataCollectors.Interfaces;

/// <summary>
/// Generic interface for exchange clients that fetch Kline data.
/// </summary>
public interface IExchangesDataCollector
{
    /// <summary>
    /// Fetches Kline (candlestick) data for a trading pair.
    /// </summary>
    /// <param name="request">The request parameters for fetching Kline data.</param>
    /// <returns>A collection of retrieved Kline data objects.
    /// If the request fails, an empty object is returned.</returns>
    Task<KlineDataRequestResponse> GetKlineData(KlineDataRequest request);

    /// <summary>
    /// Fetches Kline (candlestick) data for multiple coins.
    /// </summary>
    /// <param name="request">The request parameters for fetching Kline data.</param>
    /// <returns>A collection of retrieved Kline data objects.</returns>
    Task<IEnumerable<KlineDataRequestResponse>> GetKlineDataBatch(KlineDataBatchRequest request);

    /// <summary>
    /// Fetches all spot coins from the available exchanges.
    /// </summary>
    /// <returns>Collection of coins, listen on all of the available exchanges.
    /// If the request fails, an empty collection is returned.</returns>
    Task<IEnumerable<Coin>> GetAllCurrentActiveSpotCoins();
}
