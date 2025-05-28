using SharedLibrary.Enums;

namespace SVC_Bridge.MicroserviceClients.SvcExternal.Contracts.Requests;

/// <summary>
/// Represents the parameters required to request Kline (candlestick) data from an exchange.
/// </summary>
public record KlineDataBatchRequest
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

    /// <summary>
    /// The main coins for which Kline data must be retrieved.
    /// </summary>
    public IEnumerable<KlineDataRequestCoinMain> MainCoins { get; set; } = [];
}
