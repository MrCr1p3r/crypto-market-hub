using SharedLibrary.Models;

namespace SVC_Scheduler.MicroserviceClients.SvcBridge.Responses.KlineData;

/// <summary>
/// The response for a Kline data request.
/// </summary>
public class KlineDataResponse
{
    /// <summary>
    /// The id of the trading pair for which Kline data was retrieved.
    /// </summary>
    public int IdTradingPair { get; set; }

    /// <summary>
    /// The Kline data for the trading pair.
    /// </summary>
    public IEnumerable<Kline> Klines { get; set; } = [];
}
