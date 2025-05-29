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
    /// <returns>
    /// Success: A list of all retrieved coins.
    /// Failure: An error that occured during the retrieval of the coins.
    /// </returns>
    Task<Result<IEnumerable<Coin>>> GetAllCoins();

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

    /// <summary>
    /// Creates multiple new quote coins.
    /// </summary>
    /// <param name="quoteCoins">The collection of quote coin creation requests.</param>
    /// <returns>
    /// Success: A collection of created quote coins.
    /// Failure: An error that occurred during the creation of the quote coins.
    /// </returns>
    Task<Result<IEnumerable<TradingPairCoinQuote>>> CreateQuoteCoins(
        IEnumerable<QuoteCoinCreationRequest> quoteCoins
    );

    /// <summary>
    /// Replaces all existing trading pairs with the provided ones.
    /// </summary>
    /// <param name="requests">The collection of trading pair creation requests.</param>
    /// <returns>
    /// Success: A collection of all coins with the new trading pairs.
    /// Failure: An error that occurred during the replacement of trading pairs.
    /// </returns>
    Task<Result<IEnumerable<Coin>>> ReplaceTradingPairs(
        IEnumerable<TradingPairCreationRequest> requests
    );

    /// <summary>
    /// Deletes all coins that are neither referenced as a base nor a quote coin in any trading pair.
    /// </summary>
    /// <returns>
    /// Success: Operation completed successfully.
    /// Failure: An error that occurred during the deletion of unreferenced coins.
    /// </returns>
    Task<Result> DeleteUnreferencedCoins();
}
