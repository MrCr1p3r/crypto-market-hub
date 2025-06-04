using SharedLibrary.Enums;

namespace GUI_Crypto.ViewModels.Chart.Models;

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
    public required TradingPairCoinQuote CoinQuote { get; set; }

    /// <summary>
    /// The exchanges available for the trading pair.
    /// </summary>
    public required IEnumerable<Exchange> Exchanges { get; set; }
}
