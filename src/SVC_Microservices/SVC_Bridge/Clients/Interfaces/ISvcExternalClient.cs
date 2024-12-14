using SVC_Bridge.Models.Input;
using SVC_Bridge.Models.Output;

namespace SVC_Bridge.Clients.Interfaces;

/// <summary>
/// Interface for interacting with the SVC_External microservice.
/// </summary>
public interface ISvcExternalClient
{
    /// <summary>
    /// Fetches Kline (candlestick) data from the external service.
    /// </summary>
    /// <param name="request">The request parameters for fetching Kline data.</param>
    /// <returns>A collection of Kline data objects.</returns>
    Task<IEnumerable<KlineData>> GetKlineData(KlineDataRequest request);
}
