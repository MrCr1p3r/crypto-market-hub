namespace SharedLibrary.Models;

/// <summary>
/// Represents Kline (candlestick) data for a trading pair.
/// </summary>
public record Kline
{
    /// <summary>
    /// The opening time of the Kline in milliseconds since the Unix epoch.
    /// </summary>
    public required long OpenTime { get; set; }

    /// <summary>
    /// The opening price at the start of the Kline period.
    /// </summary>
    public required string OpenPrice { get; set; }

    /// <summary>
    /// The highest price during the Kline period.
    /// </summary>
    public required string HighPrice { get; set; }

    /// <summary>
    /// The lowest price during the Kline period.
    /// </summary>
    public required string LowPrice { get; set; }

    /// <summary>
    /// The closing price at the end of the Kline period.
    /// </summary>
    public required string ClosePrice { get; set; }

    /// <summary>
    /// The total volume traded during the Kline period.
    /// </summary>
    public required string Volume { get; set; }

    /// <summary>
    /// The closing time of the Kline in milliseconds since the Unix epoch.
    /// </summary>
    public required long CloseTime { get; set; }
}
