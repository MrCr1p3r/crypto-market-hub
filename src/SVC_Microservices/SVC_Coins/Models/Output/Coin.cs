namespace SVC_Coins.Models.Output;

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
    /// Trading pairs where this coin is the main currency.
    /// </summary>
    public IEnumerable<TradingPair> TradingPairs { get; set; } = [];
}