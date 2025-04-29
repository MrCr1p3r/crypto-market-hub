namespace SVC_Coins.ApiModels.Requests.CoinCreation;

/// <summary>
/// Represents a model for creating a new coin.
/// </summary>
public record CoinCreationRequest : CoinCreationCoinBase
{
    /// <summary>
    /// The trading pairs of the coin.
    /// </summary>
    public required IEnumerable<CoinCreationTradingPair> TradingPairs { get; set; }
}
