using SharedLibrary.Models;

namespace GUI_Crypto.MicroserviceClients.SvcExternal.Contracts.Responses;

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
