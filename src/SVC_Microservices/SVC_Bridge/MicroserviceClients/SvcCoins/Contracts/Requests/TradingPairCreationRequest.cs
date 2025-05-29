using SharedLibrary.Enums;

namespace SVC_Bridge.MicroserviceClients.SvcCoins.Contracts.Requests;

/// <summary>
/// Represents a request model for creating a new trading pair.
/// </summary>
public record TradingPairCreationRequest
{
    /// <summary>
    /// Gets or sets the ID of the main coin in the trading pair.
    /// </summary>
    public required int IdCoinMain { get; set; }

    /// <summary>
    /// Gets or sets the ID of the quote coin in the trading pair.
    /// </summary>
    public required int IdCoinQuote { get; set; }

    /// <summary>
    /// Gets or sets the exchanges, on which the trading pair is available.
    /// </summary>
    public required IEnumerable<Exchange> Exchanges { get; set; }
}
