namespace SVC_Bridge.Models.Output;

/// <summary>
/// Represents a simplified version of a quote coin used within trading pairs to avoid recursion.
/// </summary>
public class TradingPairCoinQuote
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
}
