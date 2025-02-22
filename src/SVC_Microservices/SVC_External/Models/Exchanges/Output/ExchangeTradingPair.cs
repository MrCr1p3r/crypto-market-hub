namespace SVC_External.Models.Exchanges.Output;

/// <summary>
/// Represents a trading pair on an exchange.
/// </summary>
public record ExchangeTradingPair
{
    /// <summary>
    /// The base coin in the trading pair.
    /// </summary>
    public required ExchangeTradingPairCoinQuote CoinQuote { get; set; }

    /// <summary>
    /// Collection of exchange-specific information for this trading pair.
    /// </summary>
    public IEnumerable<ExchangeTradingPairExchangeInfo> ExchangeInfos { get; set; } = [];
}
