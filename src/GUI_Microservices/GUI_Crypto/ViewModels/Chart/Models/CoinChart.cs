using SharedLibrary.Models;

namespace GUI_Crypto.ViewModels.Chart.Models;

/// <summary>
/// Represents a cryptocurrency model for chart rendering.
/// </summary>
public class CoinChart
{
    /// <summary>
    /// Unique identifier for the coin.
    /// </summary>
    public required int Id { get; set; }

    /// <summary>
    /// Symbol of the cryptocurrency (e.g., "BTC" for Bitcoin).
    /// </summary>
    public required string Symbol { get; set; }

    /// <summary>
    /// Full name of the cryptocurrency (e.g., "Bitcoin").
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Trading pair for which kline data is available.
    /// </summary>
    public required IEnumerable<TradingPair> TradingPairs { get; set; }

    /// <summary>
    /// The id of the current trading pair.
    /// </summary>
    public required string SelectedQuoteCoinSymbol { get; set; }

    /// <summary>
    /// List of Kline objects associated with the coin.
    /// </summary>
    public required IEnumerable<Kline> Klines { get; set; }
}
