using SVC_External.Models.Input;
using SVC_External.Models.Output;

namespace SVC_External.DataCollectors.Interfaces;

/// <summary>
/// Generic interface for exchange clients that fetch Kline data.
/// </summary>
public interface IExchangesDataCollector
{
    /// <summary>
    /// Fetches Kline (candlestick) data from the exchange.
    /// </summary>
    /// <param name="request">The request parameters for fetching Kline data.</param>
    /// <returns>A collection of retrieved Kline data objects.</returns>
    Task<IEnumerable<KlineData>> GetKlineData(KlineDataRequest request);

    /// <summary>
    /// Fetches all listed coin names from the available exchanges.
    /// </summary>
    /// <returns>Object that contains a collection of listed coins for each exchange.</returns>
    Task<ListedCoins> GetAllListedCoins();
}
