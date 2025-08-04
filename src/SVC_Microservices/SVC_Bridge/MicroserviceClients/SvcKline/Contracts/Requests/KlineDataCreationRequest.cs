using SharedLibrary.Models;

namespace SVC_Bridge.MicroserviceClients.SvcKline.Contracts.Requests;

/// <summary>
/// Represents Kline (candlestick) data for a trading pair for a new entry in the db.
/// </summary>
public record KlineDataCreationRequest
{
    /// <summary>
    /// Id of the trading pair for which the Kline data is recorded.
    /// </summary>
    public required int IdTradingPair { get; set; }

    /// <summary>
    /// The new kline data for the trading pair.
    /// </summary>
    public required Kline Kline { get; set; }
}
