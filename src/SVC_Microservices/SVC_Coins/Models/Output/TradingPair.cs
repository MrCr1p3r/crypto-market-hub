namespace SVC_Coins.Models.Output;

/// <summary>
/// Represents a trading pair.
/// </summary>
public class TradingPair
{
    /// <summary>
    /// Unique identifier for the trading pair.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The data of the quote coin in the trading pair.
    /// </summary>
    public required TradingPairCoin CoinQuote { get; set; }
}
