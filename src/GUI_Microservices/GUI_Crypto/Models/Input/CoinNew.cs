namespace GUI_Crypto.Models.Input;

/// <summary>
/// Represents a new coin that will be added to the database.
/// </summary>
public class CoinNew
{
    /// <summary>
    /// Symbol of the cryptocurrency (e.g., "BTC" for Bitcoin).
    /// </summary>
    public required string Symbol { get; set; }

    /// <summary>
    /// Full name of the cryptocurrency (e.g., "Bitcoin").
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Determines sorting order for quote coins.
    /// Null means that the coin is not a quote coin.
    /// </summary>
    public int? QuoteCoinPriority { get; set; }

    /// <summary>
    /// Indicates if the coin is a stablecoin.
    /// </summary>
    public bool IsStablecoin { get; set; }
}
