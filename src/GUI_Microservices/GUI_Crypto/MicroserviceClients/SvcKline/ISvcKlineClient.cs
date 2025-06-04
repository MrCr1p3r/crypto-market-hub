using FluentResults;
using GUI_Crypto.MicroserviceClients.SvcKline.Contracts.Responses;

namespace GUI_Crypto.MicroserviceClients.SvcKline;

/// <summary>
/// Interface for interacting with the SVC_Kline microservice.
/// </summary>
public interface ISvcKlineClient
{
    /// <summary>
    /// Retrieves all Kline data from the database grouped by trading pair ID.
    /// </summary>
    /// <returns>
    /// Success: A collection of KlineDataResponse objects, each containing data for one trading pair.
    /// Failure: An error that occurred during the retrieval of the Kline data.
    /// </returns>
    Task<Result<IEnumerable<KlineDataResponse>>> GetAllKlineData();
}
