using FluentResults;
using SVC_Bridge.MicroserviceClients.SvcCoins.Contracts.Requests;
using SVC_Bridge.MicroserviceClients.SvcCoins.Contracts.Responses;

namespace SVC_Bridge.MicroserviceClients.SvcCoins;

/// <summary>
/// Interface for interractions with the SVC_Coins microservice.
/// </summary>
public interface ISvcCoinsClient
{
    /// <summary>
    /// Retrieves all coins from the system.
    /// </summary>
    /// <returns>A list of all retrieved coins.</returns>
    Task<IEnumerable<Coin>> GetAllCoins();

    /// <summary>
    /// Updates the market data for multiple coins.
    /// </summary>
    /// <param name="requests">The collection of request models for updating the market data of multiple coins.</param>
    /// <returns>
    /// Success: A collection of updated coins.
    /// Failure: An error that occured during the update of the market data of the coins.
    /// </returns>
    Task<Result<IEnumerable<Coin>>> UpdateCoinsMarketData(
        IEnumerable<CoinMarketDataUpdateRequest> requests
    );
}
