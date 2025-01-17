using GUI_Crypto.Models.Output;

namespace GUI_Crypto.Models.Chart;

/// <summary>
/// Represents a cryptocurrency model for chart rendering.
/// </summary>
public class CoinChart
{
    /// <summary>
    /// Unique identifier for the coin.
    /// </summary>
    public int Id { get; set; }

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
    public IEnumerable<TradingPair> TradingPairs { get; set; } = [];

    /// <summary>
    /// The symbol of the quote coin that was used to call the kline data.
    /// </summary>
    public string SymbolCoinQuoteCurrent { get; set; } = "";

    /// <summary>
    /// List of KlineData objects associated with the coin.
    /// </summary>
    public IEnumerable<KlineDataExchange> KlineData { get; set; } = [];
}
