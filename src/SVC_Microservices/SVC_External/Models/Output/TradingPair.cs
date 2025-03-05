namespace SVC_External.Models.Output;

/// <summary>
/// Represents a trading pair on an exchange.
/// </summary>
public record TradingPair
{
    /// <summary>
    /// The base coin in the trading pair.
    /// </summary>
    public required TradingPairCoinQuote CoinQuote { get; set; }

    /// <summary>
    /// Collection of exchange-specific information for this trading pair.
    /// </summary>
    public IEnumerable<TradingPairExchangeInfo> ExchangeInfos { get; set; } = [];
}
