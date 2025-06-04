using SharedLibrary.Enums;

namespace GUI_Crypto.MicroserviceClients.SvcExternal.Contracts.Requests;

/// <summary>
/// Represents the parameters required to request Kline (candlestick) data from an exchange.
/// </summary>
public class KlineDataRequest
{
    /// <summary>
    /// The main coin of the trading pair for which Kline data is requested.
    /// </summary>
    public required KlineDataRequestCoinBase CoinMain { get; set; }

    /// <summary>
    /// The trading pair for which Kline data is requested.
    /// </summary>
    public required KlineDataRequestTradingPair TradingPair { get; set; }

    /// <summary>
    /// The interval for each Kline. All supported intervals can be found in the TimeFrame enum.
    /// </summary>
    public ExchangeKlineInterval Interval { get; set; }

    /// <summary>
    /// The optional start time for the Kline data.
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// The optional end time for the Kline data.
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// The maximum number of Kline records to retrieve for each trading pair. Maximum value is 1000 (default).
    /// </summary>
    public int Limit { get; set; } = 1000;
}
