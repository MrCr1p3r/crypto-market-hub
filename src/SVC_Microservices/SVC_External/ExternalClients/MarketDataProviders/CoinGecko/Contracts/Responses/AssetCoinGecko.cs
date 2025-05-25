using System.Text.Json.Serialization;

namespace SVC_External.ExternalClients.MarketDataProviders.CoinGecko.Contracts.Responses;

/// <summary>
/// Represents an asset from CoinGecko API.
/// </summary>
public class AssetCoinGecko
{
    /// <summary>
    /// The trading symbol of the asset (e.g., "BTC").
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    /// <summary>
    /// The market capitalization in USD.
    /// </summary>
    [JsonPropertyName("market_cap")]
    public decimal? MarketCapUsd { get; set; }

    /// <summary>
    /// The current price in USD.
    /// </summary>
    [JsonPropertyName("current_price")]
    public decimal? PriceUsd { get; set; }

    /// <summary>
    /// The price change in percentage over the last 24 hours.
    /// </summary>
    [JsonPropertyName("price_change_percentage_24h")]
    public decimal? PriceChangePercentage24h { get; set; }
}
