using SVC_Kline.Models.Input;
using SVC_Kline.Models.Output;

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
    /// <returns>A task that inserts new Kline data into the database.</returns>
    Task InsertKlineData(KlineDataNew klineData);

    /// <summary>
    /// Inserts multiple Kline data entries into the database.
    /// </summary>
    /// <param name="klineDataList">The list of KlineData objects to insert.</param>
    /// <returns>A task that inserts multiple Kline data entries into the database.</returns>

    Task InsertManyKlineData(IEnumerable<KlineDataNew> klineDataList);

    /// <summary>
    /// Retrieves all Kline data entries from the database grouped by trading pair ID.
    /// </summary>
    /// <returns>A dictionary where key is the trading pair ID and
    /// value is a collection of KlineData objects for that trading pair.</returns>
    Task<IReadOnlyDictionary<int, IEnumerable<KlineData>>> GetAllKlineData();

    /// <summary>
    /// Deletes all Kline data entries for a specific trading pair.
    /// </summary>
    /// <param name="idTradePair">The ID of the trading pair.</param>
    /// <returns>A task that deletes all Kline data for the specified trading pair.</returns>
    Task DeleteKlineDataForTradingPair(int idTradePair);

    /// <summary>
    /// Deletes all Kline data from the database and inserts the provided new data.
    /// </summary>
    /// <param name="newKlineData">The array of KlineDataNew objects to insert after clearing the table.</param>
    /// <returns>A task that replaces all Kline data in the table with the provided new data.</returns>
    Task ReplaceAllKlineData(KlineDataNew[] newKlineData);
}
