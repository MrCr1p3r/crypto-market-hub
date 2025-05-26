namespace SVC_External.ApiContracts.Requests;

/// <summary>
/// Represents the parameters required to request Kline (candlestick) data from an exchange.
/// </summary>
public class KlineDataRequest : KlineDataRequestBase
{
    /// <summary>
    /// The main coin of the trading pair for which Kline data is requested.
    /// </summary>
    public required KlineDataRequestCoinBase CoinMain { get; set; }

    /// <summary>
    /// The trading pair for which Kline data is requested.
    /// </summary>
    public required KlineDataRequestTradingPair TradingPair { get; set; }
}
