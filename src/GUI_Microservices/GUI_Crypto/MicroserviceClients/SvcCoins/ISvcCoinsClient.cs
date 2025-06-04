using FluentResults;
using GUI_Crypto.MicroserviceClients.SvcCoins.Contracts.Requests.CoinCreation;
using GUI_Crypto.MicroserviceClients.SvcCoins.Contracts.Responses;

namespace GUI_Crypto.MicroserviceClients.SvcCoins;

/// <summary>
/// Interface for interactions with the SVC_Coins microservice.
/// </summary>
public interface ISvcCoinsClient
{
    /// <summary>
    /// Retrieves all coins from the system.
    /// </summary>
    /// <returns>
    /// Success: A list of all retrieved coins.
    /// Failure: An error that occurred during the retrieval of the coins.
    /// </returns>
    Task<Result<IEnumerable<Coin>>> GetAllCoins();

    /// <summary>
    /// Retrieves a specific coin by its ID from the system.
    /// </summary>
    /// <param name="idCoin">The ID of the coin to retrieve.</param>
    /// <returns>
    /// Success: The retrieved coin.
    /// Failure: An error that occurred during the retrieval of the coin.
    /// </returns>
    Task<Result<Coin>> GetCoinById(int idCoin);

    /// <summary>
    /// Creates multiple new coins along with their trading pairs.
    /// </summary>
    /// <param name="coins">The collection of creation requests.</param>
    /// <returns>
    /// Success: A collection of created coins.
    /// Failure: An error that occurred during the creation of the coins.
    /// </returns>
    Task<Result<IEnumerable<Coin>>> CreateCoins(IEnumerable<CoinCreationRequest> coins);

    /// <summary>
    /// Deletes a main coin from the system.
    /// </summary>
    /// <param name="idCoin">The ID of the main coin that should be deleted.</param>
    /// <returns>
    /// Success: Operation completed successfully.
    /// Failure: An error that occurred during the deletion of the main coin.
    /// </returns>
    Task<Result> DeleteMainCoin(int idCoin);

    /// <summary>
    /// Deletes all coins (and, via cascade, any related data) from the system.
    /// </summary>
    /// <returns>
    /// Success: Operation completed successfully.
    /// Failure: An error that occurred during the deletion of all coins.
    /// </returns>
    Task<Result> DeleteAllCoins();
}
