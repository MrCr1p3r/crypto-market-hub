using FluentResults;
using GUI_Crypto.ApiContracts.Requests.KlineData;
using GUI_Crypto.ServiceModels;
using SharedLibrary.Models;

namespace GUI_Crypto.Services.Chart;

/// <summary>
/// Defines contract for service for handling chart-related operations.
/// </summary>
public interface IChartService
{
    /// <summary>
    /// Retrieves all data needed for the chart view model.
    /// </summary>
    /// <param name="idCoin">The id of the coin to fetch chart data for.</param>
    /// <param name="idTradingPair">The id of the trading pair to fetch chart data for.</param>
    /// <returns>
    /// Success: Aggregated chart data.
    /// Failure: An error that occurred during data retrieval.
    /// </returns>
    Task<Result<ChartData>> GetChartData(int idCoin, int idTradingPair);

    /// <summary>
    /// Fetches kline data for a specific trading pair from external exchanges.
    /// </summary>
    /// <param name="request">The request parameters for fetching Kline data.</param>
    /// <returns>
    /// Success: Successfully fetched Kline data.
    /// Failure: An error that occurred during data retrieval.
    /// </returns>
    Task<Result<IEnumerable<Kline>>> GetKlineData(KlineDataRequest request);
}
