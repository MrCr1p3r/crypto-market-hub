using SharedLibrary.Enums;

namespace SVC_Bridge.MicroserviceClients.SvcExternal.Contracts.Responses.Coins;

/// <summary>
/// Represents a base class for coin.
/// </summary>
public abstract class CoinBase
{
    /// <summary>
    /// Symbol of the cryptocurrency (e.g., "BTC" for Bitcoin). Is always uppercase.
    /// </summary>
    public required string Symbol { get; set; }

    /// <summary>
    /// Full name of the cryptocurrency (e.g., "Bitcoin").
    /// </summary>
    /// <remarks>
    /// Null if no name for this coin could be retrieved.
    /// </remarks>
    public string? Name { get; set; }

    /// <summary>
    /// Category of the coin.
    /// </summary>
    /// <remarks>
    /// Null if this coin is a regular coin/token.
    /// </remarks>
    public CoinCategory? Category { get; set; }

    /// <summary>
    /// Id of the coin in the CoinGecko API.
    /// </summary>
    public string? IdCoinGecko { get; set; }
}
