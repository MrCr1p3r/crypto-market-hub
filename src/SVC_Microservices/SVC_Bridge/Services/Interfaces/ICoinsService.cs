using FluentResults;
using SVC_Bridge.ApiContracts.Responses;

namespace SVC_Bridge.Services.Interfaces;

/// <summary>
/// Defines the interface for managing coin operations.
/// </summary>
public interface ICoinsService
{
    /// <summary>
    /// Updates the market data for all coins in the system using market data from external sources.
    /// </summary>
    /// <returns>
    /// A Result object indicating success or containing errors if the operation failed.
    /// Success: Updated market data of the coins.
    /// Failure: Result containing error details about what went wrong during the update process.
    /// </returns>
    Task<Result<IEnumerable<CoinMarketData>>> UpdateCoinsMarketData();
}
