using FluentResults;
using SVC_Bridge.MicroserviceClients.SvcExternal.Contracts.Responses;

namespace SVC_Bridge.MicroserviceClients.SvcExternal;

/// <summary>
/// Interface for interacting with the SVC_External microservice.
/// </summary>
public interface ISvcExternalClient
{
    /// <summary>
    /// Retrieves market data for specified CoinGecko coin IDs.
    /// </summary>
    /// <param name="coinGeckoIds">Collection of CoinGecko coin IDs to fetch data for.</param>
    /// <returns>
    /// Success: Collection of coin asset information including price, market cap, and stablecoin status.
    /// Failure: An error that occured during the retrieval of the market data.
    /// </returns>
    Task<Result<IEnumerable<CoinGeckoAssetInfo>>> GetCoinGeckoAssetsInfo(
        IEnumerable<string> coinGeckoIds
    );
}
