namespace GUI_Crypto.ApiContracts.Responses.OverviewCoin;

/// <summary>
/// Represents Kline (candlestick) data for a trading pair.
/// </summary>
public class KlineData
{
    /// <summary>
    /// Trading pair for which klines are available.
    /// </summary>
    public required TradingPair TradingPair { get; set; }

    /// <summary>
    /// The klines for the trading pair.
    /// </summary>
    public required IEnumerable<Kline> Klines { get; set; } = [];
}
