using SharedLibrary.Enums;

namespace SVC_External.Models.Output;

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
    /// TODO: Do I need status if I always return active trading pairs?
    public ExchangeTradingPairStatus Status { get; set; }
}
