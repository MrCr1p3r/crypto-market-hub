namespace SVC_External.Models.MarketDataProviders.Input;

/// <summary>
/// Request model for getting exchange tickers.
/// </summary>
public class CoinGeckoIdsRequest
{
    /// <summary>
    /// The exchange identifier.
    /// </summary>
    public string IdExchange { get; set; } = string.Empty;

    /// <summary>
    /// A collection of symbols to get the CoinGecko IDs for.
    /// </summary>
    public HashSet<string> Symbols { get; set; } = [];
}
