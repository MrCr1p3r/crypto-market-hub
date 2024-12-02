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
    /// <returns>A task that inserts the coin into the database.</returns>
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
    /// <returns>A task that deletes the coin from the database.</returns>
    Task DeleteCoin(int idCoin);

    /// <summary>
    /// Inserts a new trading pair entry into the database.
    /// </summary>
    /// <param name="tradingPair">The trading pair object to insert.</param>
    /// <returns>The ID of the inserted trading pair.</returns>
    Task<int> InsertTradingPair(TradingPairNew tradingPair);
}
