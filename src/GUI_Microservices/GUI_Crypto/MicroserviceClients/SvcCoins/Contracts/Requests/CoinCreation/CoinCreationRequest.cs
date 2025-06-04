namespace GUI_Crypto.MicroserviceClients.SvcCoins.Contracts.Requests.CoinCreation;

/// <summary>
/// Represents a model for creating a new coin.
/// </summary>
public record CoinCreationRequest : CoinCreationCoinBase
{
    /// <summary>
    /// Id of the main coin.
    /// </summary>
    /// <remarks>
    /// If not provided, this main coin will be added to the database.
    /// If provided, it will be updated from quote coin to the main coin.
    /// </remarks>
    public int? Id { get; set; }

    /// <summary>
    /// The trading pairs of the coin.
    /// </summary>
    public required IEnumerable<CoinCreationTradingPair> TradingPairs { get; set; }
}
