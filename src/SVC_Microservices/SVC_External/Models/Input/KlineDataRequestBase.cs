using SharedLibrary.Enums;

namespace SVC_External.Models.Input;

/// <summary>
/// Represents the base class with parameters required to retrieve Kline (candlestick) data from an exchange.
/// </summary>
public class KlineDataRequestBase
{
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
