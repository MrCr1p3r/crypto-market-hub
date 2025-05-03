namespace SVC_Coins.ApiModels.Responses;

/// <summary>
/// Represents a cryptocurrency model.
/// </summary>
public class Coin : CoinBase
{
    /// <summary>
    /// Gets or sets trading pairs where this coin is the main currency.
    /// </summary>
    public IEnumerable<TradingPair> TradingPairs { get; set; } = [];
}
