using SharedLibrary.Enums;

namespace SVC_External.Models.Exchanges.Input;

/// <summary>
/// Represents the formatted parameters required to request Kline data exchange.
/// </summary>
public record ExchangeKlineDataRequest
{
    /// <summary>
    /// The base coin in the trading pair for which Kline data is requested.
    /// </summary>
    public required string CoinMainSymbol { get; set; }

    /// <summary>
    /// The quote coin in the trading pair for which Kline data is requested.
    /// </summary>
    public required string CoinQuoteSymbol { get; set; }

    /// <summary>
    /// The interval for each Kline. All supported intervals can be found in the TimeFrame enum.
    /// </summary>
    public ExchangeKlineInterval Interval { get; set; }

    /// <summary>
    /// The opening time of the Kline in milliseconds since the Unix epoch.
    /// </summary>
    public long StartTimeUnix { get; set; }

    /// <summary>
    /// The closing time of the Kline in milliseconds since the Unix epoch.
    /// </summary>
    public long EndTimeUnix { get; set; }

    /// <summary>
    /// The maximum number of Kline records to retrieve. Default is 100.
    /// </summary>
    public int Limit { get; set; } = 100;
}
