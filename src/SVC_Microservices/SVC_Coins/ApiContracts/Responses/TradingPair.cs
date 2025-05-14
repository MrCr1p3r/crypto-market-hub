using SharedLibrary.Enums;

namespace SVC_Coins.ApiContracts.Responses;

/// <summary>
/// Represents a trading pair.
/// </summary>
public class TradingPair
{
    /// <summary>
    /// Gets or sets unique identifier for the trading pair.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the data of the quote coin in the trading pair.
    /// </summary>
    public required TradingPairCoinQuote CoinQuote { get; set; }

    /// <summary>
    /// Exchanges, on which this trading pair is available.
    /// </summary>
    public required IEnumerable<Exchange> Exchanges { get; set; }
}
