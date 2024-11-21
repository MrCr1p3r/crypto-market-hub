using SharedLibrary.Enums;

namespace SVC_External.Models.Input;

/// <summary>
/// Represents the parameters required to request Kline (candlestick) data from an exchange.
/// </summary>
public class KlineDataRequest
{
    /// <summary>
    /// The base coin in the trading pair for which Kline data is requested.
    /// </summary>
    public required string CoinMain { get; set; }

    /// <summary>
    /// The quote coin in the trading pair for which Kline data is requested.
    /// </summary>
    public required string CoinQuote { get; set; }
    
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
    /// The maximum number of Kline records to retrieve. Default is 100.
    /// </summary>
    public int Limit { get; set; } = 100;
}
