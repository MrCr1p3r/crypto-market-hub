namespace SharedLibrary.Enums;

/// <summary>
/// Represents the status of a trading pair tradability on an exchange.
/// </summary>
public enum ExchangeTradingPairStatus
{
    /// <summary>
    /// The trading pair is available.
    /// </summary>
    Available,

    /// <summary>
    /// The trading pair is temporarily unavailable.
    /// </summary>
    CurrentlyUnavailable,

    /// <summary>
    /// The trading pair is permanently unavailable.
    /// </summary>
    Unavailable,
}
