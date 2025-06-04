using FluentResults;
using GUI_Crypto.ApiContracts.Requests.CoinCreation;
using GUI_Crypto.ApiContracts.Responses.CandidateCoin;
using GUI_Crypto.ApiContracts.Responses.OverviewCoin;

namespace GUI_Crypto.Services.Interfaces;

/// <summary>
/// Defines the available operations for the overview service.
/// </summary>
public interface IOverviewService
{
    /// <summary>
    /// Retrieves all coins from the system for the overview page.
    /// </summary>
    /// <returns>
    /// Success: Collection of all overview coins with associated kline data.
    /// Failure: An error that occurred during the retrieval.
    /// </returns>
    Task<Result<IEnumerable<OverviewCoin>>> GetOverviewCoins();

    /// <summary>
    /// Retrieves coins that are candidates for insertion into the database.
    /// </summary>
    /// <returns>
    /// Success: Collection of all available candidate coins.
    /// Failure: An error that occurred during the retrieval.
    /// </returns>
    Task<Result<IEnumerable<CandidateCoin>>> GetCandidateCoins();

    /// <summary>
    /// Creates multiple new coins along with their trading pairs.
    /// </summary>
    /// <param name="requests">The collection of coin creation requests.</param>
    /// <returns>
    /// Success: Collection of created coins.
    /// Failure: An error that occurred during the creation.
    /// </returns>
    Task<Result<IEnumerable<OverviewCoin>>> CreateCoins(IEnumerable<CoinCreationRequest> requests);

    /// <summary>
    /// Deletes a specific coin from the system.
    /// </summary>
    /// <param name="idCoin">The ID of the coin to delete.</param>
    /// <returns>
    /// Success: Operation completed successfully.
    /// Failure: An error that occurred during the deletion.
    /// </returns>
    Task<Result> DeleteMainCoin(int idCoin);

    /// <summary>
    /// Deletes all coins (and related data) from the system.
    /// </summary>
    /// <returns>
    /// Success: Operation completed successfully.
    /// Failure: An error that occurred during the deletion.
    /// </returns>
    Task<Result> DeleteAllCoins();
}
