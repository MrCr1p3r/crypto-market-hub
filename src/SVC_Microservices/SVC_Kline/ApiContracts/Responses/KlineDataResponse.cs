using SharedLibrary.Models;

namespace SVC_Kline.ApiContracts.Responses;

/// <summary>
/// Represents kline data api response format.
/// </summary>
public class KlineDataResponse
{
    /// <summary>
    /// Id of the trading pair for which kline data is available.
    /// </summary>
    public int IdTradingPair { get; set; }

    /// <summary>
    /// The klines for the trading pair.
    /// </summary>
    public IEnumerable<Kline> Klines { get; set; } = [];
}
