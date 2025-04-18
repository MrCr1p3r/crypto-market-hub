using SVC_Coins.Domain.Entities;
using SVC_Coins.Domain.ValueObjects;

namespace SVC_Coins.Repositories.Interfaces;

/// <summary>
/// Interface for the repository that handles operations related to Coins.
/// </summary>
public interface ICoinsRepository
{
    /// <summary>
    /// Retrieves all coins from the database.
    /// </summary>
    /// <returns>A collection of coin entities.</returns>
    Task<IEnumerable<CoinsEntity>> GetAllCoins();

    /// <summary>
    /// Retrieves a collection of coins by their IDs.
    /// </summary>
    /// <param name="ids">The IDs of the coins to retrieve.</param>
    /// <returns>A collection of found coin entities.</returns>
    Task<IEnumerable<CoinsEntity>> GetCoinsByIds(IEnumerable<int> ids);

    /// <summary>
    /// Retrieves a collection of coins that match the specified symbol-name pairs.
    /// </summary>
    /// <param name="pairs">The collection of symbol-name pairs to match.</param>
    /// <returns>A collection of found coin entities.</returns>
    Task<IEnumerable<CoinsEntity>> GetCoinsBySymbolNamePairs(IEnumerable<CoinSymbolNamePair> pairs);

    /// <summary>
    /// Inserts multiple new Coin entities into the database.
    /// </summary>
    /// <param name="coins">The collection of Coin entities to insert.</param>
    /// <returns>A collection of inserted Coin entities.</returns>
    Task<IEnumerable<CoinsEntity>> InsertCoins(IEnumerable<CoinsEntity> coins);

    /// <summary>
    /// Updates the data of multiple coins.
    /// </summary>
    /// <param name="coins">The collection of coins with updated data.</param>
    /// <returns>A collection of coins with updated data.</returns>
    Task<IEnumerable<CoinsEntity>> UpdateCoins(IEnumerable<CoinsEntity> coins);

    /// <summary>
    /// Deletes a coin from the database.
    /// </summary>
    /// <param name="coin">The coin to delete.</param>
    /// <returns>A task, that deletes the coin.</returns>
    Task DeleteCoin(CoinsEntity coin);

    /// <summary>
    /// Clears all data from the Coins and TradingPairs tables in the database.
    /// </summary>
    /// <returns>Task, that clears the database.</returns>
    Task DeleteAllCoinsWithRelatedData();
}
