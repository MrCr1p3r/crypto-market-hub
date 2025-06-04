namespace GUI_Crypto.ApiContracts.Responses.CandidateCoin;

/// <summary>
/// Represents a candidate coin for insertion into the database.
/// </summary>
public class CandidateCoin : CandidateCoinBase
{
    /// <summary>
    /// Trading pairs where this coin is the main currency.
    /// </summary>
    public IEnumerable<CandidateCoinTradingPair> TradingPairs { get; set; } = [];
}
