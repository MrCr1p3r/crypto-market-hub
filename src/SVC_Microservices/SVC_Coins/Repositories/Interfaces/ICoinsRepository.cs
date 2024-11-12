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
    Task InsertCoin(CoinNew coin);

    /// <summary>
    /// Retrieves all coins from the database.
    /// </summary>
    /// <returns>A collection of coin objects.</returns>
    Task<IEnumerable<Coin>> GetAllCoins();

    /// <summary>
    /// Deletes a coin from the database.
    /// </summary>
    /// <param name="idCoin">The ID of the coin to delete.</param>
    Task DeleteCoin(int idCoin);

    /// <summary>
    /// Inserts a new trading pair entry into the database.
    /// </summary>
    /// <param name="tradingPair">The trading pair object to insert.</param>
    Task InsertTradingPair(TradingPairNew tradingPair);
}
