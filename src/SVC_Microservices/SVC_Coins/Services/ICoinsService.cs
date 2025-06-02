using FluentResults;
using SVC_Coins.ApiContracts.Requests;
using SVC_Coins.ApiContracts.Requests.CoinCreation;
using SVC_Coins.ApiContracts.Responses;

namespace SVC_Coins.Services;

/// <summary>
/// Contract for coins-related business operations.
/// </summary>
public interface ICoinsService
{
    /// <summary>
    /// Retrieves all coins from the system.
    /// </summary>
    /// <returns>A collection of retrieved coins.</returns>
    Task<IEnumerable<Coin>> GetAllCoins();

    /// <summary>
    /// Retrieves a collection of coins by their IDs.
    /// </summary>
    /// <param name="ids">The IDs of the coins to retrieve.</param>
    /// <returns>A collection of found coins.</returns>
    Task<IEnumerable<Coin>> GetCoinsByIds(IEnumerable<int> ids);

    /// <summary>
    /// Creates multiple new coins along with their trading pairs.
    /// </summary>
    /// <param name="requests">The collection of requests with the new coin data.</param>
    /// <returns>
    /// Success: A collection of created coins.
    /// Failure: A collection of occurred errors.
    /// </returns>
    Task<Result<IEnumerable<Coin>>> CreateCoinsWithTradingPairs(
        IEnumerable<CoinCreationRequest> requests
    );

    /// <summary>
    /// Creates multiple new quote coins.
    /// </summary>
    /// <param name="requests">The collection of requests with the new quote coin data.</param>
    /// <returns>
    /// Success: A collection of created quote coins.
    /// Failure: A collection of occurred errors.
    /// </returns>
    Task<Result<IEnumerable<TradingPairCoinQuote>>> CreateQuoteCoins(
        IEnumerable<QuoteCoinCreationRequest> requests
    );

    /// <summary>
    /// Updates the market data of multiple coins in the system.
    /// </summary>
    /// <param name="requests">The collection of request models for updating the market data of multiple coins.</param>
    /// <returns>
    /// Success: A collection of updated coins.
    /// Failure: A collection of occurred errors.
    /// </returns>
    Task<Result<IEnumerable<Coin>>> UpdateCoinsMarketData(
        IEnumerable<CoinMarketDataUpdateRequest> requests
    );

    /// <summary>
    /// Replaces all trading pairs in the system with the new ones.
    /// </summary>
    /// <param name="requests">The collection of trading pairs to replace.</param>
    /// <returns>
    /// Success: A collection of coins with new trading pairs.
    /// Failure: A collection of occurred errors.
    /// </returns>
    Task<Result<IEnumerable<Coin>>> ReplaceAllTradingPairs(
        IEnumerable<TradingPairCreationRequest> requests
    );

    /// <summary>
    /// Downgrades the specified coin from main coin to quote coin by deleting its trading pairs
    /// and removes it completely from the system if it is not referenced by any trading pairs.
    /// </summary>
    /// <param name="idCoin">The ID of the main coin to delete.</param>
    /// <returns>
    /// Success: A result representing the outcome of the operation.
    /// Failure: A collection of occurred errors.
    /// </returns>
    Task<Result> DeleteMainCoin(int idCoin);

    /// <summary>
    /// Deletes all coins that are neither referenced as a main nor a quote coin in any trading pair.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteCoinsNotReferencedByTradingPairs();

    /// <summary>
    /// Deletes all coins and their related data from the system.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteAllCoinsWithRelatedData();
}
