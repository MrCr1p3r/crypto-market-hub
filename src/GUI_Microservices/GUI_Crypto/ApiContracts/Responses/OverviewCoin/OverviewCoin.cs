using SharedLibrary.Enums;

namespace GUI_Crypto.ApiContracts.Responses.OverviewCoin;

/// <summary>
/// Represents a cryptocurrency model for the overview page.
/// </summary>
public class OverviewCoin : CoinBase
{
    /// <summary>
    /// Category of the coin.
    /// </summary>
    /// <remarks>
    /// Null if this coin is a regular coin/token.
    /// </remarks>
    public CoinCategory? Category { get; set; }

    /// <summary>
    /// The market capitalization in USD.
    /// </summary>
    public long? MarketCapUsd { get; init; }

    /// <summary>
    /// The current price in USD.
    /// </summary>
    public string? PriceUsd { get; init; }

    /// <summary>
    /// The price change in percentage over the last 24 hours.
    /// </summary>
    public decimal? PriceChangePercentage24h { get; init; }

    /// <summary>
    /// Kline data for the coin.
    /// </summary>
    public KlineData? KlineData { get; set; }
}
