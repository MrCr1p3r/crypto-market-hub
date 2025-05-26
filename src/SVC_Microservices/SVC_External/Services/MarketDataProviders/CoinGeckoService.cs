using FluentResults;
using SVC_External.ApiContracts.Responses.MarketDataProviders;
using SVC_External.ExternalClients.MarketDataProviders.CoinGecko;
using SVC_External.ExternalClients.MarketDataProviders.CoinGecko.Contracts.Responses;
using SVC_External.Services.MarketDataProviders.Interfaces;
using static SharedLibrary.Errors.GenericErrors;

namespace SVC_External.Services.MarketDataProviders;

/// <summary>
/// Implementation of the CoinGecko service that fetches data from CoinGecko API.
/// </summary>
public class CoinGeckoService(ICoinGeckoClient coinGeckoClient) : ICoinGeckoService
{
    private readonly ICoinGeckoClient _coinGeckoClient = coinGeckoClient;

    /// <inheritdoc />
    public async Task<Result<IEnumerable<CoinGeckoAssetInfo>>> GetCoinGeckoAssetsInfo(
        IEnumerable<string> ids
    )
    {
        var stablecoinInfosTask = _coinGeckoClient.GetMarketDataForCoins(ids);
        var stablecoinIdsTask = _coinGeckoClient.GetStablecoinsIds();
        await Task.WhenAll(stablecoinInfosTask, stablecoinIdsTask);

        var stablecoinInfos = await stablecoinInfosTask;
        if (stablecoinInfos.IsFailed)
        {
            return Result.Fail(
                new InternalError(
                    $"Failed to retrieve stablecoin infos.",
                    reasons: stablecoinInfos.Errors
                )
            );
        }

        var stablecoinIds = await stablecoinIdsTask;
        return stablecoinIds.IsFailed
            ? Result.Fail(
                new InternalError(
                    $"Failed to retrieve stablecoin IDs.",
                    reasons: stablecoinIds.Errors
                )
            )
            : Result.Ok(
                stablecoinInfos.Value.Select(info =>
                    Mapping.ToCoinGeckoAssetInfo(info, stablecoinIds.Value)
                )
            );
    }

    private static class Mapping
    {
        public static CoinGeckoAssetInfo ToCoinGeckoAssetInfo(
            AssetCoinGecko coinGeckoAssetInfo,
            IEnumerable<string> stablecoinIds
        ) =>
            new()
            {
                Id = coinGeckoAssetInfo.Id,
                MarketCapUsd = Convert.ToInt32(coinGeckoAssetInfo.MarketCapUsd),
                PriceUsd = coinGeckoAssetInfo.PriceUsd,
                PriceChangePercentage24h = coinGeckoAssetInfo.PriceChangePercentage24h,
                IsStablecoin = stablecoinIds.Contains(coinGeckoAssetInfo.Id),
            };
    }
}
