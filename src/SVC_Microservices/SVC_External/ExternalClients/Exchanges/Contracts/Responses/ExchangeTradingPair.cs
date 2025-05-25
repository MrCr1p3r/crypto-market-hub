namespace SVC_External.ExternalClients.Exchanges.Contracts.Responses;

/// <summary>
/// Represents a trading pair on an exchange.
/// </summary>
public record ExchangeTradingPair
{
    /// <summary>
    /// The quote coin in the trading pair.
    /// </summary>
    public required ExchangeTradingPairCoinQuote CoinQuote { get; set; }

    /// <summary>
    /// Exchange-specific information for this trading pair.
    /// </summary>
    public required ExchangeTradingPairExchangeInfo ExchangeInfo { get; set; }
}
