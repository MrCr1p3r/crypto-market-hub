using SVC_External.Models.Input;
using SVC_External.Models.Output;

namespace SVC_External.Clients.Interfaces;

/// <summary>
/// Generic interface for exchange clients that fetch Kline data.
/// </summary>
public interface IExchangeClient
{
    /// <summary>
    /// Fetches Kline (candlestick) data from the exchange.
    /// </summary>
    /// <param name="request">The request parameters for fetching Kline data.</param>
    /// <returns>A collection of retrieved Kline data objects.</returns>
    Task<IEnumerable<KlineData>> GetKlineData(KlineDataRequestFormatted request);
}