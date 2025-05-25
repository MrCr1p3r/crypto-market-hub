namespace SVC_External.ExternalClients.MarketDataProviders.CoinGecko.Contracts.Responses;

/// <summary>
/// Represents a coin from CoinGecko API.
/// </summary>
public class CoinCoinGecko
{
    /// <summary>
    /// The CoinGecko ID of the coin.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// The trading symbol of the coin (e.g., "BTC").
    /// </summary>
    public required string Symbol { get; set; }

    /// <summary>
    /// The human-readable name of the coin (e.g., "Bitcoin").
    /// </summary>
    public required string Name { get; set; }
}
