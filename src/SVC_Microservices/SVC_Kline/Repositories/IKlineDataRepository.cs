using SVC_Kline.ApiContracts.Requests;
using SVC_Kline.ApiContracts.Responses;

namespace SVC_Kline.Repositories;

/// <summary>
/// Interface for the repository that handles operations related to Kline data.
/// </summary>
public interface IKlineDataRepository
{
    /// <summary>
    /// Retrieves all Kline data entries from the database grouped by trading pair ID.
    /// </summary>
    /// <returns>A dictionary where key is the trading pair ID and
    /// value is a collection of KlineData objects for that trading pair.</returns>
    Task<IReadOnlyDictionary<int, IEnumerable<KlineData>>> GetAllKlineData();

    /// <summary>
    /// Inserts multiple Kline data entries into the database.
    /// </summary>
    /// <param name="klineDataList">The list of KlineData objects to insert.</param>
    /// <returns>A task that inserts multiple Kline data entries into the database.</returns>
    Task InsertKlineData(IEnumerable<KlineDataCreationRequest> klineDataList);

    /// <summary>
    /// Deletes all Kline data from the database and inserts the provided new data.
    /// </summary>
    /// <param name="newKlineData">The array of KlineDataNew objects to insert after clearing the table.</param>
    /// <returns>A task that replaces all Kline data in the table with the provided new data.</returns>
    Task ReplaceAllKlineData(KlineDataCreationRequest[] newKlineData);
}
