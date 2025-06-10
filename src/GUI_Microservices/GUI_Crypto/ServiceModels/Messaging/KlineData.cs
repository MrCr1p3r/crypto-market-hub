using SharedLibrary.Models;

namespace GUI_Crypto.ServiceModels.Messaging;

/// <summary>
/// Represents Kline (candlestick) data for a trading pair.
/// </summary>
public class KlineData
{
    /// <summary>
    /// The id of the trading pair for which Kline data was retrieved.
    /// </summary>
    public required int IdTradingPair { get; set; }

    /// <summary>
    /// The klines for the trading pair.
    /// </summary>
    public required IEnumerable<Kline> Klines { get; set; } = [];
}
