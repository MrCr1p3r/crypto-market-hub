namespace SVC_Bridge.MicroserviceClients.SvcExternal.Contracts.Responses;

/// <summary>
/// Represents an asset from CoinGecko API.
/// </summary>
public class CoinGeckoAssetInfo
{
    /// <summary>
    /// The coingecko id of the asset.
    /// </summary>
    public required string Id { get; set; }

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

    /// <summary>
    /// Indicates if the asset is a stablecoin.
    /// </summary>
    public bool IsStablecoin { get; set; }
}
