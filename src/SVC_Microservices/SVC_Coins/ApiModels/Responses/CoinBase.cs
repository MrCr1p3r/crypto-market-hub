using SharedLibrary.Enums;

namespace SVC_Coins.ApiModels.Responses;

/// <summary>
/// Represents a base cryptocurrency model.
/// </summary>
public abstract class CoinBase
{
    /// <summary>
    /// Gets or sets unique identifier for the coin.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets symbol of the cryptocurrency (e.g., "BTC" for Bitcoin). Is always uppercase.
    /// </summary>
    public required string Symbol { get; set; }

    /// <summary>
    /// Gets or sets full name of the cryptocurrency (e.g., "Bitcoin").
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Category of the coin.
    /// </summary>
    /// <remarks>
    /// Null if this coin is a regular coin/token.
    /// </remarks>
    public CoinCategory? Category { get; set; }

    /// <summary>
    /// Id of the coin in the CoinGecko API.
    /// </summary>
    public string? IdCoinGecko { get; set; }

    /// <summary>
    /// The market capitalization in USD.
    /// </summary>
    public int? MarketCapUsd { get; init; }

    /// <summary>
    /// The current price in USD.
    /// </summary>
    public string? PriceUsd { get; init; }

    /// <summary>
    /// The price change in percentage over the last 24 hours.
    /// </summary>
    public decimal? PriceChangePercentage24h { get; init; }
}
