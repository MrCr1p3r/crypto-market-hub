namespace SVC_External.Models.Output;

/// <summary>
/// Represents a collection of coins listed on various exchanges.
/// </summary>
public class ListedCoins
{
    /// <summary>
    /// Coins that are currently listed on Binance.
    /// </summary>
    public IEnumerable<string> BinanceCoins { get; set; } = [];

    /// <summary>
    /// Coins that are currently listed on ByBit.
    /// </summary>
    public IEnumerable<string> BybitCoins { get; set; } = [];

    /// <summary>
    /// Coins that are currently listed on Mexc.
    /// </summary>
    public IEnumerable<string> MexcCoins { get; set; } = [];
}
