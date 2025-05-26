namespace SVC_External.ApiContracts.Responses.Exchanges.Coins;

/// <summary>
/// Represents a trading pair on an exchange.
/// </summary>
public record TradingPair
{
    /// <summary>
    /// The quote coin in the trading pair.
    /// </summary>
    public required TradingPairCoinQuote CoinQuote { get; set; }

    /// <summary>
    /// Collection of exchange-specific information for this trading pair.
    /// </summary>
    public IEnumerable<TradingPairExchangeInfo> ExchangeInfos { get; set; } = [];
}
