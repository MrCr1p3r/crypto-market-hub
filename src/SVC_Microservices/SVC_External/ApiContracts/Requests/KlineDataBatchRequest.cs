namespace SVC_External.ApiContracts.Requests;

/// <summary>
/// Represents the parameters required to request Kline (candlestick) data from an exchange.
/// </summary>
public class KlineDataBatchRequest : KlineDataRequestBase
{
    /// <summary>
    /// The main coins for which Kline data must be retrieved.
    /// </summary>
    public IEnumerable<KlineDataRequestCoinMain> MainCoins { get; set; } = [];
}
