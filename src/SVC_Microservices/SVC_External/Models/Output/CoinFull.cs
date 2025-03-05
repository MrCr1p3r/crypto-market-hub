namespace SVC_External.Models.Output;

/// <summary>
/// Represents a cryptocurrency model.
/// </summary>
public class CoinFull : CoinBase
{
    /// <summary>
    /// Price of the coin in USD.
    /// </summary>
    public decimal? PriceUsd { get; set; }

    /// <summary>
    /// Market cap of the coin in USD.
    /// </summary>
    public long? MarketCapUsd { get; set; }

    /// <summary>
    /// Trading pairs where this coin is the main currency.
    /// </summary>
    public IEnumerable<TradingPair> TradingPairs { get; set; } = [];
}
