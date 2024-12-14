namespace SVC_Bridge.Models.Input;

/// <summary>
/// Represents a data for creating a new trading pair.
/// </summary>
public class TradingPairNew
{
    /// <summary>
    /// Foreign key referencing the main coin in the trading pair.
    /// </summary>
    public int IdCoinMain { get; set; }

    /// <summary>
    /// Foreign key referencing the quote coin in the trading pair.
    /// </summary>
    public int IdCoinQuote { get; set; }
}
