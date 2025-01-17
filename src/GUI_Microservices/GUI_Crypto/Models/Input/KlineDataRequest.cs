using SharedLibrary.Enums;

namespace GUI_Crypto.Models.Input;

/// <summary>
/// Represents the parameters required to request Kline (candlestick) data from an exchange.
/// </summary>
public class KlineDataRequest
{
    /// <summary>
    /// The base coin symbol in the trading pair for which Kline data is requested.
    /// </summary>
    public required string CoinMainSymbol { get; set; }

    /// <summary>
    /// The quote coin symbol in the trading pair for which Kline data is requested.
    /// </summary>
    public required string CoinQuoteSymbol { get; set; }

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
    /// The maximum number of Kline records to retrieve. Maximum value is 1000 (default).
    /// </summary>
    public int Limit { get; set; } = 1000;
}
