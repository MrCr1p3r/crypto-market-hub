using FluentResults;
using SVC_External.ApiContracts.Responses.MarketDataProviders;

namespace SVC_External.Services.MarketDataProviders.Interfaces;

/// <summary>
/// Service interface for managing CoinGecko data operations.
/// </summary>
public interface ICoinGeckoService
{
    /// <summary>
    /// Fetches coingecko asset information for the provided ids.
    /// </summary>
    /// <param name="ids">Collection of CoinGecko IDs.</param>
    /// <returns>
    /// Success: Result containing a collection of coingecko asset information objects. <br/>
    /// Failure: Result with an error object describing the failure inside.
    /// </returns>
    Task<Result<IEnumerable<CoinGeckoAssetInfo>>> GetCoinGeckoAssetsInfo(IEnumerable<string> ids);
}
