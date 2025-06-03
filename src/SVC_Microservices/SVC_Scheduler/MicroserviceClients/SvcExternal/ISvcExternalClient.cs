using FluentResults;
using SVC_Scheduler.MicroserviceClients.SvcExternal.Contracts.Responses.Coins;

namespace SVC_Scheduler.MicroserviceClients.SvcExternal;

/// <summary>
/// Interface for interacting with the SVC_External microservice.
/// </summary>
public interface ISvcExternalClient
{
    /// <summary>
    /// Retrieves all spot coins from all available exchanges.
    /// </summary>
    /// <returns>
    /// Success: Collection of all active spot coins from all exchanges.
    /// Failure: An error that occurred during the retrieval of the spot coins.
    /// </returns>
    Task<Result<IEnumerable<Coin>>> GetAllSpotCoins();
}
