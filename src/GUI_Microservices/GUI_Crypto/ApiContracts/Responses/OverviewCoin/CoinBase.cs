namespace GUI_Crypto.ApiContracts.Responses.OverviewCoin;

/// <summary>
/// Represents a base model for overview coins.
/// </summary>
public abstract class CoinBase
{
    /// <summary>
    /// Gets or sets unique identifier for the coin.
    /// </summary>
    public required int Id { get; set; }

    /// <summary>
    /// Gets or sets symbol of the cryptocurrency (e.g., "BTC" for Bitcoin). Is always uppercase.
    /// </summary>
    public required string Symbol { get; set; }

    /// <summary>
    /// Gets or sets full name of the cryptocurrency (e.g., "Bitcoin").
    /// </summary>
    public required string Name { get; set; }
}
