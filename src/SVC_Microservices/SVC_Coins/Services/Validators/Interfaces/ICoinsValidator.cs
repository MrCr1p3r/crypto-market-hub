using FluentResults;
using SVC_Coins.ApiModels.Requests;
using SVC_Coins.ApiModels.Requests.CoinCreation;

namespace SVC_Coins.Services.Validators.Interfaces;

/// <summary>
/// Defines the contract for validating coin-related data.
/// </summary>
public interface ICoinsValidator
{
    /// <summary>
    /// Validates a collection of coin creation requests.
    /// </summary>
    /// <param name="requests">Collection of coin creation requests to validate.</param>
    /// <returns>
    /// Success: Empty success result.
    /// Failure: List of validation errors.
    /// </returns>
    Task<Result> ValidateCoinCreationRequests(IEnumerable<CoinCreationRequest> requests);

    /// <summary>
    /// Validates market data update requests.
    /// </summary>
    /// <param name="requests">Collection of coin market data update requests to validate.</param>
    /// <returns>
    /// Success: Empty success result.
    /// Failure: List of validation errors.
    /// </returns>
    Task<Result> ValidateMarketDataUpdateRequests(
        IEnumerable<CoinMarketDataUpdateRequest> requests
    );
}
