using FluentResults;
using SVC_Bridge.ApiContracts.Responses.Coins;

namespace SVC_Bridge.Services.Interfaces;

/// <summary>
/// Defines the interface for managing trading pairs operations.
/// </summary>
public interface ITradingPairsService
{
    /// <summary>
    /// Updates the trading pairs in the system by synchronizing with external data sources.
    /// </summary>
    /// <returns>
    /// A Result object indicating success or containing errors if the operation failed.
    /// Success: Collection of coins with new trading pairs.
    /// Failure: Result containing error details about what went wrong during the update process.
    /// </returns>
    Task<Result<IEnumerable<Coin>>> UpdateTradingPairs();
}
