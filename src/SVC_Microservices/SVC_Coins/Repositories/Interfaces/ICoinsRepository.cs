using FluentResults;
using SVC_Coins.Models.Input;
using SVC_Coins.Models.Output;

namespace SVC_Coins.Repositories.Interfaces;

/// <summary>
/// Interface for the repository that handles operations related to Coins.
/// </summary>
public interface ICoinsRepository
{
    /// <summary>
    /// Inserts a new Coin entry into the database.
    /// </summary>
    /// <param name="coin">The Coin object to insert.</param>
    /// <returns>A result representing the outcome of the operation.</returns>
    Task<Result> InsertCoin(CoinNew coin);

    /// <summary>
    /// Inserts multiple new Coin entries into the database.
    /// </summary>
    /// <param name="coins">The collection of Coin objects to insert.</param>
    /// <returns>A result representing the outcome of the operation.</returns>
    Task<Result> InsertCoins(IEnumerable<CoinNew> coins);

    /// <summary>
    /// Retrieves all coins from the database.
    /// </summary>
    /// <returns>A collection of coin objects.</returns>
    Task<IEnumerable<Coin>> GetAllCoins();

    /// <summary>
    /// Deletes a coin from the database.
    /// </summary>
    /// <param name="idCoin">The ID of the coin to delete.</param>
    /// <returns>A result representing the outcome of the operation.</returns>
    Task<Result> DeleteCoin(int idCoin);

    /// <summary>
    /// Inserts a new trading pair entry into the database.
    /// </summary>
    /// <param name="tradingPair">The trading pair object to insert.</param>
    /// <returns>A result representing the outcome of the operation.
    /// If operation was successful, returns the ID of the inserted trading pair.</returns>
    Task<Result<int>> InsertTradingPair(TradingPairNew tradingPair);

    /// <summary>
    /// Retrieves a collection of quote coins sorted by priority from database.
    /// </summary>
    /// <returns>A collection of quote coins sorted by priority.</returns>
    Task<IEnumerable<Coin>> GetQuoteCoinsPrioritized();

    /// <summary>
    /// Retrieves a collection of coins by their IDs.
    /// </summary>
    /// <param name="ids">The IDs of the coins to retrieve.</param>
    /// <returns>A collection of coins.</returns>
    Task<IEnumerable<Coin>> GetCoinsByIds(IEnumerable<int> ids);
}
