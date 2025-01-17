using GUI_Crypto.Models.Output;

namespace GUI_Crypto.Models.Overview;

/// <summary>
/// Represents a cryptocurrency model.
/// </summary>
public class OverviewCoin
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
    public TradingPair? TradingPair { get; set; }

    /// <summary>
    /// List of KlineData objects associated with the coin.
    /// </summary>
    public IEnumerable<KlineData> KlineData { get; set; } = [];
}
