using LanguageExt.Common;
using SVC_Bridge.Models.Input;

namespace SVC_Bridge.DataCollectors.Interfaces;

/// <summary>
/// Interface for the Kline data collection operations.
/// </summary>
public interface IKlineDataCollector
{
    /// <summary>
    /// Updates the entire Kline data for all coins.
    /// </summary>
    /// <returns>A task that replaces all the kline data in the database with new data.</returns>
    Task<IEnumerable<KlineDataNew>> CollectEntireKlineData();
}
