using System.Text.Json.Serialization;

namespace SVC_External.Models.MarketDataProviders.Output;

/// <summary>
/// Represents an asset from CoinGecko API.
/// </summary>
public class AssetCoinGecko
{
    /// <summary>
    /// The trading symbol of the asset (e.g., "BTC").
    /// </summary>
    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// The human-readable name of the asset (e.g., "Bitcoin").
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The market capitalization in USD.
    /// </summary>
    [JsonPropertyName("market_cap")]
    public decimal MarketCapUsd { get; set; }

    /// <summary>
    /// The current price in USD.
    /// </summary>
    [JsonPropertyName("current_price")]
    public decimal PriceUsd { get; set; }
}
