using SharedLibrary.Enums;

namespace GUI_Crypto.MicroserviceClients.SvcExternal.Contracts.Requests;

/// <summary>
/// A trading pair, for which Kline data will be retrieved.
/// </summary>
public class KlineDataRequestTradingPair
{
    /// <summary>
    /// The id of the trading pair.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The quote coin of the trading pair.
    /// </summary>
    public required KlineDataRequestCoinQuote CoinQuote { get; set; }

    /// <summary>
    /// The exchanges on which this trading pair is traded.
    /// </summary>
    public required IEnumerable<Exchange> Exchanges { get; set; }
}
