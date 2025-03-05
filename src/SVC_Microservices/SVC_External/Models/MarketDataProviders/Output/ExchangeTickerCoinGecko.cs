using System.Text.Json.Serialization;

namespace SVC_External.Models.MarketDataProviders.Output;

/// <summary>
/// Represents a coin from CoinGecko API.
/// </summary>
public class ExchangeTickerCoinGecko
{
    /// <summary>
    /// The CoinGecko ID of the coin.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The trading symbol of the coin (e.g., "BTC").
    /// </summary>
    public string Symbol { get; set; } = string.Empty;
}
