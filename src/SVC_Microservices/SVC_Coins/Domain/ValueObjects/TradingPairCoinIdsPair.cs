namespace SVC_Coins.Domain.ValueObjects;

/// <summary>
/// Represents a trading pair's main coin and quote coin identifier pair.
/// </summary>
public readonly record struct TradingPairCoinIdsPair
{
    /// <summary>
    /// Gets or initializes the ID of the main coin in the trading pair.
    /// </summary>
    public required int IdCoinMain { get; init; }

    /// <summary>
    /// Gets or initializes the ID of the quote coin in the trading pair.
    /// </summary>
    public required int IdCoinQuote { get; init; }
}
