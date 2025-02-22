namespace SVC_External.Models.Exchanges.Output;

/// <summary>
/// Represents a quote coin of the trading pair retrieved from an exchange.
/// </summary>
public class ExchangeTradingPairCoinQuote
{
    /// <summary>
    /// Symbol of the cryptocurrency (e.g., "BTC" for Bitcoin). Is always uppercase.
    /// </summary>
    public required string Symbol { get; set; }

    /// <summary>
    /// Full name of the cryptocurrency (e.g., "Bitcoin").
    /// </summary>
    /// <remarks>
    /// Null if no name for this coin could be retrieved.
    /// </remarks>
    public string? Name { get; set; }
}
