namespace GUI_Crypto.Models.Chart;

/// <summary>
/// Represents a cryptocurrency model which data will be used to fetch the data needed
/// for the chart rendering.
/// </summary>
public class CoinChartRequest
{
    /// <summary>
    /// Unique identifier for the coin.
    /// </summary>
    public int IdCoinMain { get; set; }

    /// <summary>
    /// Symbol of the cryptocurrency (e.g., "BTC" for Bitcoin).
    /// </summary>
    public required string SymbolCoinMain { get; set; }

    /// <summary>
    /// Symbol of the cryptocurrency (e.g., "BTC" for Bitcoin).
    /// </summary>
    public required string SymbolCoinQuote { get; set; }
}
