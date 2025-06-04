namespace GUI_Crypto.MicroserviceClients.SvcExternal.Contracts.Requests;

/// <summary>
/// A base class for a kline data request coin.
/// </summary>
public class KlineDataRequestCoinBase
{
    /// <summary>
    /// The id of the coin.
    /// </summary>
    public required int Id { get; set; }

    /// <summary>
    /// The symbol of the coin.
    /// </summary>
    public required string Symbol { get; set; }

    /// <summary>
    /// The name of the coin.
    /// </summary>
    public required string Name { get; set; }
}
