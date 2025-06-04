using FluentResults;
using GUI_Crypto.MicroserviceClients.SvcExternal.Contracts.Requests;
using GUI_Crypto.MicroserviceClients.SvcExternal.Contracts.Responses.Coins;
using GUI_Crypto.MicroserviceClients.SvcExternal.Contracts.Responses.KlineData;

namespace GUI_Crypto.MicroserviceClients.SvcExternal;

/// <summary>
/// Interface for interacting with the SVC_External microservice.
/// </summary>
public interface ISvcExternalClient
{
    /// <summary>
    /// Retrieves all spot coins from all available exchanges.
    /// </summary>
    /// <returns>
    /// Success: Collection of all active spot coins from all exchanges.
    /// Failure: An error that occurred during the retrieval of the spot coins.
    /// </returns>
    Task<Result<IEnumerable<Coin>>> GetAllSpotCoins();

    /// <summary>
    /// Fetches Kline (candlestick) data for a specific trading pair from available exchanges.
    /// </summary>
    /// <param name="request">The request parameters for fetching Kline data.</param>
    /// <returns>
    /// Success: A Kline data response containing trading pair ID and kline data.
    /// Failure: An error that occurred during the retrieval of the Kline data.
    /// </returns>
    Task<Result<KlineDataResponse>> GetKlineData(KlineDataRequest request);
}
