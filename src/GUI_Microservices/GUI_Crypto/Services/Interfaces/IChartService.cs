using FluentResults;
using GUI_Crypto.ApiContracts.Requests.KlineData;
using GUI_Crypto.ApiContracts.Responses;
using GUI_Crypto.ServiceModels;

namespace GUI_Crypto.Services.Interfaces;

/// <summary>
/// Defines contract for service for handling chart-related operations.
/// </summary>
public interface IChartService
{
    /// <summary>
    /// Retrieves all data needed for the chart view model.
    /// </summary>
    /// <param name="request">The coin chart parameters containing coin identifiers.</param>
    /// <returns>
    /// Success: Aggregated chart data.
    /// Failure: An error that occurred during data retrieval.
    /// </returns>
    Task<Result<ChartData>> GetChartData(KlineDataRequest request);

    /// <summary>
    /// Fetches kline data for a specific trading pair from external exchanges.
    /// </summary>
    /// <param name="request">The request parameters for fetching Kline data.</param>
    /// <returns>
    /// Success: Successfully fetched Kline data.
    /// Failure: An error that occurred during data retrieval.
    /// </returns>
    Task<Result<IEnumerable<KlineData>>> GetKlineData(KlineDataRequest request);
}
