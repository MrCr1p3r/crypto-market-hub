namespace SVC_Coins.ApiContracts.Requests;

/// <summary>
/// Represents a request model for updating the market data of a coin.
/// </summary>
public class CoinMarketDataUpdateRequest
{
    /// <summary>
    /// Gets or sets unique identifier for the coin.
    /// </summary>
    public required int Id { get; set; }

    /// <summary>
    /// The market capitalization in USD.
    /// </summary>
    public long? MarketCapUsd { get; set; }

    /// <summary>
    /// The current price in USD.
    /// </summary>
    public decimal? PriceUsd { get; set; }

    /// <summary>
    /// The price change in percentage over the last 24 hours.
    /// </summary>
    public decimal? PriceChangePercentage24h { get; set; }
}
