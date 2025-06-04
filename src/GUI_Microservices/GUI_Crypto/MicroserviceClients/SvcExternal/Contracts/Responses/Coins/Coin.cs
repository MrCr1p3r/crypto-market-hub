namespace GUI_Crypto.MicroserviceClients.SvcExternal.Contracts.Responses.Coins;

/// <summary>
/// Represents a cryptocurrency model.
/// </summary>
public class Coin : CoinBase
{
    /// <summary>
    /// Trading pairs where this coin is the main currency.
    /// </summary>
    public IEnumerable<TradingPair> TradingPairs { get; set; } = [];
}
