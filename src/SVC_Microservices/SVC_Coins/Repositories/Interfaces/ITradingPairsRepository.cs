using SVC_Coins.Domain.Entities;
using SVC_Coins.Domain.ValueObjects;

namespace SVC_Coins.Repositories.Interfaces;

/// <summary>
/// Interface for the repository that handles operations related to TradingPairs.
/// </summary>
public interface ITradingPairsRepository
{
    /// <summary>
    /// Retrieves a collection of trading pairs by their coin IDs.
    /// </summary>
    /// <param name="pairs">The collection of coin IDs pairs.</param>
    /// <returns>A collection of found trading pair entities.</returns>
    Task<IEnumerable<TradingPairsEntity>> GetTradingPairsByCoinIdPairs(
        IEnumerable<TradingPairCoinIdsPair> pairs
    );

    /// <summary>
    /// Inserts multiple new trading pair entities into the database.
    /// </summary>
    /// <param name="tradingPairs">The collection of trading pair entities to insert.</param>
    /// <returns>A collection of inserted trading pair entities.</returns>
    Task<IEnumerable<TradingPairsEntity>> InsertTradingPairs(
        IEnumerable<TradingPairsEntity> tradingPairs
    );

    /// <summary>
    /// Replaces all trading pairs in the database with the provided trading pairs.
    /// </summary>
    /// <param name="tradingPairs">The collection of trading pair entities to replace the existing ones with.</param>
    /// <returns>A task, that replaces all trading pairs in the database with the provided ones.</returns>
    Task ReplaceAllTradingPairs(IEnumerable<TradingPairsEntity> tradingPairs);

    /// <summary>
    /// Deletes all trading pairs where the specified coin is the main coin.
    /// </summary>
    /// <param name="idCoin">The ID of the main coin to delete the trading pairs for.</param>
    /// <returns>A task, that deletes trading pairs where the specified coin is the main coin.</returns>
    Task DeleteMainCoinTradingPairs(int idCoin);
}
