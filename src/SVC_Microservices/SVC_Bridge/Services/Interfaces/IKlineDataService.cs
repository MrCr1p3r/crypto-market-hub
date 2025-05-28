using FluentResults;
using SVC_Bridge.ApiContracts.Responses.KlineData;

namespace SVC_Bridge.Services.Interfaces;

/// <summary>
/// Defines the interface for managing kline data operations.
/// </summary>
public interface IKlineDataService
{
    /// <summary>
    /// Updates the kline data for all coins in the system.
    /// </summary>
    /// <returns>
    /// A Result object indicating success or containing errors if the operation failed.
    /// Success: Updated kline data grouped by trading pairs.
    /// Failure: Result containing error details about what went wrong during the update process.
    /// </returns>
    Task<Result<IEnumerable<KlineDataResponse>>> UpdateKlineData();
}
