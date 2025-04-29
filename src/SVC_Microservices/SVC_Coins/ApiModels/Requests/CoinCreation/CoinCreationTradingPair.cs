using SharedLibrary.Enums;

namespace SVC_Coins.ApiModels.Requests.CoinCreation;

/// <summary>
/// Represents trading pair's data of a new coin.
/// </summary>
public record CoinCreationTradingPair
{
    /// <summary>
    /// The quote coin in the trading pair.
    /// </summary>
    public required CoinCreationCoinQuote CoinQuote { get; set; }

    /// <summary>
    /// The collection of exchanges on which the trading pair is available.
    /// </summary>
    public required IEnumerable<Exchange> Exchanges { get; set; }
}
