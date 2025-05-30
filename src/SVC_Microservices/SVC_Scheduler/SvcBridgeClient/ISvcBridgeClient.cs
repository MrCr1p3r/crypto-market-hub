using FluentResults;
using SVC_Scheduler.SvcBridgeClient.Responses;
using SVC_Scheduler.SvcBridgeClient.Responses.Coins;
using SVC_Scheduler.SvcBridgeClient.Responses.KlineData;

namespace SVC_Scheduler.SvcBridgeClient;

/// <summary>
/// Interface for interactions with the SVC_Bridge microservice.
/// </summary>
public interface ISvcBridgeClient
{
    /// <summary>
    /// Updates the market data for all coins in the system.
    /// </summary>
    /// <returns>
    /// Success: A collection of updated coin market data.
    /// Failure: An error that occurred during the market data update operation.
    /// </returns>
    Task<Result<IEnumerable<CoinMarketData>>> UpdateCoinsMarketData();

    /// <summary>
    /// Updates the kline data for all coins in the system.
    /// </summary>
    /// <returns>
    /// Success: A collection of updated kline data grouped by trading pairs.
    /// Failure: An error that occurred during the kline data update operation.
    /// </returns>
    Task<Result<IEnumerable<KlineDataResponse>>> UpdateKlineData();

    /// <summary>
    /// Updates all trading pairs for all coins in the system.
    /// </summary>
    /// <returns>
    /// Success: A collection of coins with updated trading pairs.
    /// Failure: An error that occurred during the trading pairs update operation.
    /// </returns>
    Task<Result<IEnumerable<Coin>>> UpdateTradingPairs();
}
