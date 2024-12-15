namespace SVC_Coins.Models.Input;

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
}
