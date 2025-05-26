using SharedLibrary.Enums;

namespace SVC_External.ApiContracts.Responses.Exchanges.Coins;

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
