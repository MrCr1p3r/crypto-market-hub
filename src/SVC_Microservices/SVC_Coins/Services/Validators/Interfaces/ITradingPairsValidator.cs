using FluentResults;
using SVC_Coins.ApiModels.Requests;

namespace SVC_Coins.Services.Validators.Interfaces;

/// <summary>
/// Defines the contract for validating trading pair-related operations.
/// </summary>
public interface ITradingPairsValidator
{
    /// <summary>
    /// Validates a collection of trading pair replacement requests.
    /// </summary>
    /// <param name="requests">Collection of trading pair creation requests to validate.</param>
    /// <returns>A list of validation error messages. Empty list means validation passed.</returns>
    Task<Result> ValidateTradingPairsReplacementRequests(
        IEnumerable<TradingPairCreationRequest> requests
    );
}
