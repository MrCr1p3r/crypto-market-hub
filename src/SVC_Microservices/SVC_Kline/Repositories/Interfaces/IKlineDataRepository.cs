using SVC_Kline.Models.Input;

namespace SVC_Kline.Repositories.Interfaces;

/// <summary>
/// Interface for the repository that handles operations related to Kline data.
/// </summary>
public interface IKlineDataRepository
{
    /// <summary>
    /// Inserts a new Kline data entry into the database.
    /// </summary>
    /// <param name="klineData">The KlineData object to insert.</param>
    Task InsertKlineData(KlineData klineData);

    /// <summary>
    /// Inserts multiple Kline data entries into the database.
    /// </summary>
    /// <param name="klineDataList">The list of KlineData objects to insert.</param>
    Task InsertManyKlineData(IEnumerable<KlineData> klineDataList);

    /// <summary>
    /// Retrieves all Kline data entries from the database.
    /// </summary>
    /// <returns>A collection of KlineData objects.</returns>
    Task<IEnumerable<KlineData>> GetAllKlineData();

    /// <summary>
    /// Deletes all Kline data entries for a specific trading pair.
    /// </summary>
    /// <param name="idTradePair">The ID of the trading pair.</param>
    Task DeleteKlineDataForTradingPair(int idTradePair);
}
