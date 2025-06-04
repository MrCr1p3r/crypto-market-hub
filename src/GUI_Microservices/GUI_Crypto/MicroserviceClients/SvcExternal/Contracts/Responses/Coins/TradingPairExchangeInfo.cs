using SharedLibrary.Enums;

namespace GUI_Crypto.MicroserviceClients.SvcExternal.Contracts.Responses.Coins;

/// <summary>
/// Represents trading pair information specific to an exchange.
/// </summary>
public record TradingPairExchangeInfo
{
    /// <summary>
    /// The exchange where this trading pair is listed.
    /// </summary>
    public Exchange Exchange { get; set; }

    /// <summary>
    /// The status of the trading pair tradability on this exchange.
    /// </summary>
    public ExchangeTradingPairStatus Status { get; set; }
}
