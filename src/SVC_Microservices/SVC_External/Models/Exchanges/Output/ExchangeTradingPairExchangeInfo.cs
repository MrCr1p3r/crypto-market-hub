using SharedLibrary.Enums;

namespace SVC_External.Models.Exchanges.Output;

/// <summary>
/// Represents trading pair information specific to an exchange.
/// </summary>
public record ExchangeTradingPairExchangeInfo
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
