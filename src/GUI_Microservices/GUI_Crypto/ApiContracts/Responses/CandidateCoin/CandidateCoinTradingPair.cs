using SharedLibrary.Enums;

namespace GUI_Crypto.ApiContracts.Responses.CandidateCoin;

/// <summary>
/// Represents a trading pair on an exchange.
/// </summary>
public record CandidateCoinTradingPair
{
    /// <summary>
    /// The quote coin in the trading pair.
    /// </summary>
    public required CandidateCoinTradingPairCoinQuote CoinQuote { get; set; }

    /// <summary>
    /// Collection of exchanges, on which this trading pair is available.
    /// </summary>
    public IEnumerable<Exchange> Exchanges { get; set; } = [];
}
