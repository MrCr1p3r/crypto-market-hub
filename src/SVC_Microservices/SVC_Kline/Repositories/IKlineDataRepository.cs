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
    /// <returns>A collection of KlineDataResponse objects,
    /// each containing data for one trading pair.</returns>
    Task<IEnumerable<KlineDataResponse>> GetAllKlineData();

    /// <summary>
    /// Inserts multiple Kline data entries into the database.
    /// </summary>
    /// <param name="klineDataList">The list of KlineDataCreationRequest objects to insert.</param>
    /// <returns>A collection of KlineDataResponse objects,
    /// each containing new kline data for one trading pair.</returns>
    Task<IEnumerable<KlineDataResponse>> InsertKlineData(
        IEnumerable<KlineDataCreationRequest> klineDataList
    );

    /// <summary>
    /// Deletes all Kline data from the database and inserts the provided new data.
    /// </summary>
    /// <param name="newKlineData">The array of KlineDataCreationRequest objects to insert after clearing the table.</param>
    /// <returns>A collection of KlineDataResponse objects,
    /// each containing new kline data for one trading pair.</returns>
    Task<IEnumerable<KlineDataResponse>> ReplaceAllKlineData(
        KlineDataCreationRequest[] newKlineData
    );
}
