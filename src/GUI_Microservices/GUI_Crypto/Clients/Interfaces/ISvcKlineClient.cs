using GUI_Crypto.Models.Input;
using GUI_Crypto.Models.Output;

namespace GUI_Crypto.Clients.Interfaces;

/// <summary>
/// Interface for the interractions with the SVC_Kline microservice.
/// </summary>
public interface ISvcKlineClient
{
    /// <summary>
    /// Inserts new Kline data.
    /// </summary>
    /// <param name="klineData">The KlineData to insert.</param>
    /// <returns>A task that inserts new Kline data.</returns>
    Task InsertKlineData(KlineDataNew klineData);

    /// <summary>
    /// Inserts multiple Kline data entries.
    /// </summary>
    /// <param name="klineDataList">A list of KlineData objects to insert.</param>
    /// <returns>A task that inserts multiple Kline data objects.</returns>
    Task InsertManyKlineData(IEnumerable<KlineDataNew> klineDataList);

    /// <summary>
    /// Retrieves all Kline data.
    /// </summary>
    /// <returns>A list of all Kline data.</returns>
    Task<IEnumerable<KlineData>> GetAllKlineData();

    /// <summary>
    /// Deletes all Kline data for a specific trading pair.
    /// </summary>
    /// <param name="idTradePair">The ID of the trading pair.</param>
    /// <returns>A task that deletes all Kline data for a specific trading pair.</returns>
    Task DeleteKlineDataForTradingPair(int idTradePair);
}
