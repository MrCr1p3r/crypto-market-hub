using GUI_Crypto.Models.Input;
using GUI_Crypto.Models.Output;
using SVC_External.Models.Output;

namespace GUI_Crypto.Clients.Interfaces;

/// <summary>
/// Interface for the interractions with the SVC_External microservice.
/// </summary>
public interface ISvcExternalClient
{
    /// <summary>
    /// Fetches Kline (candlestick) data from the external service.
    /// </summary>
    /// <param name="request">The request parameters for fetching Kline data.</param>
    /// <returns>A collection of Kline data objects.</returns>
    Task<IEnumerable<KlineDataExchange>> GetKlineData(KlineDataRequest request);

    /// <summary>
    /// Retrieves all listed coins from the external service.
    /// </summary>
    /// <returns>A collection of coins listed on each exchange.</returns>
    Task<ListedCoins> GetAllListedCoins();
}
