using SharedLibrary.Enums;

namespace GUI_Crypto.Models.Input;

/// <summary>
/// Represents the parameters required to update Kline (candlestick).
/// </summary>
public class KlineDataUpdateRequest
{
    /// <summary>
    /// The interval for each Kline. All supported intervals can be found in the TimeFrame enum.
    /// </summary>
    public ExchangeKlineInterval Interval { get; set; } = ExchangeKlineInterval.FourHours;

    /// <summary>
    /// The optional start time for the Kline data.
    /// </summary>
    public DateTime StartTime { get; set; } = DateTime.Now.AddDays(-7);

    /// <summary>
    /// The optional end time for the Kline data.
    /// </summary>
    public DateTime EndTime { get; set; } = DateTime.Now;

    /// <summary>
    /// The maximum number of Kline records to retrieve. Default is 100.
    /// </summary>
    public int Limit { get; set; } = 100;
}
