using GUI_Crypto.Models.Input;

namespace GUI_Crypto.Clients.Interfaces;

/// <summary>
/// Interface for the interactions with the SVC_Bridge microservice.
/// </summary>
public interface ISvcBridgeClient
{
    /// <summary>
    /// Updates the entire Kline data for all coins.
    /// </summary>
    /// <param name="request">The kline data request parameters.</param>
    /// <returns>A task that updates the entire Kline data.</returns>
    Task UpdateEntireKlineData(KlineDataUpdateRequest request);
}
