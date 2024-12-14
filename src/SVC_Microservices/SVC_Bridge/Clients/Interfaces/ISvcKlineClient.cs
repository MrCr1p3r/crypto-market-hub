using SVC_Bridge.Models.Input;

namespace SVC_Bridge.Clients.Interfaces;

/// <summary>
/// Interface for interacting with the SVC_Kline microservice.
/// </summary>
public interface ISvcKlineClient
{
    /// <summary>
    /// Deletes all Kline data and inserts the provided new data.
    /// </summary>
    /// <param name="newKlineData">The array of KlineDataNew objects to insert instead of old data.</param>
    /// <returns>A task that replaces all Kline data with the provided new data.</returns>
    Task ReplaceAllKlineData(IEnumerable<KlineDataNew> newKlineData);
}
