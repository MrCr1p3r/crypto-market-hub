using SVC_External.Models.Exchanges.Input;
using SVC_External.Models.Exchanges.Output;

namespace SVC_External.Clients.Exchanges.Interfaces;

/// <summary>
/// Generic interface for exchange clients that fetch Kline data.
/// </summary>
public interface IExchangesClient
{
    /// <summary>
    /// Fetches all listed coins from the exchange.
    /// </summary>
    /// <returns>Collection of coins, listed on spot market of this exchange.</returns>
    Task<IEnumerable<ExchangeCoin>> GetAllSpotCoins();

    /// <summary>
    /// Fetches Kline (candlestick) data from the exchange.
    /// </summary>
    /// <param name="request">The request parameters for fetching Kline data.</param>
    /// <returns>A collection of retrieved Kline data objects.</returns>
    Task<IEnumerable<ExchangeKlineData>> GetKlineData(ExchangeKlineDataRequest request);
}
