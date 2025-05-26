using FluentResults;
using SVC_External.ApiContracts.Responses.Exchanges.Coins;

namespace SVC_External.Services.Exchanges.Interfaces;

/// <summary>
/// Service interface for managing coin data from exchanges.
/// </summary>
public interface ICoinsService
{
    /// <summary>
    /// Fetches all spot coins from the available exchanges.
    /// </summary>
    /// <returns>
    /// Success: Result containing a collection of coins, listed on all of the available exchanges. <br/>
    /// Failure: Result with an error object describing the failure inside.
    /// </returns>
    Task<Result<IEnumerable<Coin>>> GetAllCurrentActiveSpotCoins();
}
