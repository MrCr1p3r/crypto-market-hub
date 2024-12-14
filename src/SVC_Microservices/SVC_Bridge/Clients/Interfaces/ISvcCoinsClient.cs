using SVC_Bridge.Models.Input;
using SVC_Bridge.Models.Output;

namespace SVC_Bridge.Clients.Interfaces;

/// <summary>
/// Interface for interractions with the SVC_Coins microservice.
/// </summary>
public interface ISvcCoinsClient
{
    /// <summary>
    /// Retrieves all coins from the database.
    /// </summary>
    /// <returns>A list of all coins entries.</returns>
    Task<IEnumerable<Coin>> GetAllCoins();

    /// <summary>
    /// Inserts a new trading pair into the database.
    /// </summary>
    /// <param name="tradingPair">The trading pair object to insert.</param>
    /// <returns>The ID of the inserted trading pair.</returns>
    Task<int> InsertTradingPair(TradingPairNew tradingPair);
}
