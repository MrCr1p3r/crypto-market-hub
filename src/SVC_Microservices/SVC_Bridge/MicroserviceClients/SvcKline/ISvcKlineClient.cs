using FluentResults;
using SVC_Bridge.MicroserviceClients.SvcKline.Contracts.Requests;
using SVC_Bridge.MicroserviceClients.SvcKline.Contracts.Responses;

namespace SVC_Bridge.MicroserviceClients.SvcKline;

/// <summary>
/// Interface for interacting with the SVC_Kline microservice.
/// </summary>
public interface ISvcKlineClient
{
    /// <summary>
    /// Replaces all kline data in the system with the new provided ones.
    /// </summary>
    /// <param name="newKlineData">
    /// The array of KlineDataNew objects to insert instead of old data.
    /// </param>
    /// <returns>
    /// Success: Result object containing the collection of new kline data grouped by trading pairs.
    /// Failure: An error that occured during the replacement of the kline data.
    /// </returns>
    Task<Result<IEnumerable<KlineDataResponse>>> ReplaceKlineData(
        IEnumerable<KlineDataCreationRequest> newKlineData
    );
}
