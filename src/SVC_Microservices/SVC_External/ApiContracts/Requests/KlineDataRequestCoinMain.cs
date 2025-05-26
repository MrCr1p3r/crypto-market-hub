namespace SVC_External.ApiContracts.Requests;

/// <summary>
/// A main coin, for which Kline data must be retrieved.
/// </summary>
public class KlineDataRequestCoinMain : KlineDataRequestCoinBase
{
    /// <summary>
    /// The trading pairs which will be used to retrieve Kline data.
    /// </summary>
    public required IEnumerable<KlineDataRequestTradingPair> TradingPairs { get; set; }
}
