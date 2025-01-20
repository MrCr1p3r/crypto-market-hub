namespace SVC_Bridge.Models.Output;

/// <summary>
/// Represents a cryptocurrency model.
/// </summary>
public class Coin
{
    /// <summary>
    /// Unique identifier for the coin.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Symbol of the cryptocurrency (e.g., "BTC" for Bitcoin).
    /// </summary>
    public required string Symbol { get; set; }

    /// <summary>
    /// Full name of the cryptocurrency (e.g., "Bitcoin").
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Determines sorting order for quote coins.
    /// Null means that the coin is not a quote coin.
    /// </summary>
    public int? QuoteCoinPriority { get; set; }

    /// <summary>
    /// Indicates if the coin is a stablecoin.
    /// </summary>
    public bool IsStablecoin { get; set; }

    /// <summary>
    /// Trading pairs where this coin is the main currency.
    /// </summary>
    public IEnumerable<TradingPair> TradingPairs { get; set; } = [];
}
