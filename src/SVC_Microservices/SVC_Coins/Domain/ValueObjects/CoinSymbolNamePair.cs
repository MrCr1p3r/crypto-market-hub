namespace SVC_Coins.Domain.ValueObjects;

/// <summary>
/// Represents a coin's unique symbol and name identifier pair.
/// </summary>
public readonly record struct CoinSymbolNamePair
{
    /// <summary>
    /// Gets or initializes the symbol of the coin (e.g., "BTC", "ETH").
    /// </summary>
    public required string Symbol { get; init; }

    /// <summary>
    /// Gets or initializes the name of the coin (e.g., "Bitcoin", "Ethereum").
    /// </summary>
    public required string Name { get; init; }
}
