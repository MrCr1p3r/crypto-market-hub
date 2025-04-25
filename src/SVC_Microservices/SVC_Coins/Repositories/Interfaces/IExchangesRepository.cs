using SVC_Coins.Domain.Entities;

namespace SVC_Coins.Repositories.Interfaces;

/// <summary>
/// Interface for the repository that handles operations related to Exchanges.
/// </summary>
public interface IExchangesRepository
{
    /// <summary>
    /// Retrieves all exchanges from the database.
    /// </summary>
    /// <returns>A collection of exchange entities.</returns>
    Task<IEnumerable<ExchangesEntity>> GetAllExchanges();
}
