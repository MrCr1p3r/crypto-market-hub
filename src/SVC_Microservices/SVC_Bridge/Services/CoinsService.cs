using FluentResults;
using SVC_Bridge.ApiContracts.Responses;
using SVC_Bridge.MicroserviceClients.SvcCoins;
using SVC_Bridge.MicroserviceClients.SvcCoins.Contracts.Requests;
using SVC_Bridge.MicroserviceClients.SvcCoins.Contracts.Responses;
using SVC_Bridge.MicroserviceClients.SvcExternal;
using SVC_Bridge.MicroserviceClients.SvcExternal.Contracts.Responses;
using SVC_Bridge.Services.Interfaces;
using static SharedLibrary.Errors.GenericErrors;

namespace SVC_Bridge.Services;

/// <summary>
/// Service for managing coin operations.
/// </summary>
public class CoinsService(ISvcCoinsClient svcCoinsClient, ISvcExternalClient svcExternalClient)
    : ICoinsService
{
    private readonly ISvcCoinsClient _svcCoinsClient = svcCoinsClient;
    private readonly ISvcExternalClient _svcExternalClient = svcExternalClient;

    /// <inheritdoc />
    public async Task<Result<IEnumerable<CoinMarketData>>> UpdateCoinsMarketData()
    {
        // Step 1: Get all coins from the coins service
        var coinsResult = await _svcCoinsClient.GetAllCoins();
        if (coinsResult.IsFailed)
        {
            return Result.Fail(
                new InternalError(
                    "Failed to retrieve coins from coins service.",
                    reasons: coinsResult.Errors
                )
            );
        }

        var coins = coinsResult.Value;

        // Step 2: Extract CoinGecko IDs from coins that have them
        var coinGeckoIds = coins
            .Where(coin => !string.IsNullOrWhiteSpace(coin.IdCoinGecko))
            .Select(coin => coin.IdCoinGecko!);
        if (!coinGeckoIds.Any())
        {
            return Result.Ok(Enumerable.Empty<CoinMarketData>());
        }

        // Step 3: Get market data from CoinGecko using the CoinGecko IDs
        var marketDataResult = await _svcExternalClient.GetCoinGeckoAssetsInfo(coinGeckoIds);
        if (marketDataResult.IsFailed)
        {
            return Result.Fail(
                new InternalError(
                    "Failed to retrieve CoinGecko assets info.",
                    reasons: marketDataResult.Errors
                )
            );
        }

        // Step 4: Map market data to update requests
        var updateRequests = Mapping.ToUpdateRequests(coins, marketDataResult.Value);
        if (!updateRequests.Any())
        {
            return Result.Ok(Enumerable.Empty<CoinMarketData>());
        }

        // Step 5: Update coins market data
        var updateResult = await _svcCoinsClient.UpdateCoinsMarketData(updateRequests);
        if (updateResult.IsFailed)
        {
            return Result.Fail(
                new InternalError(
                    "Failed to update coins market data.",
                    reasons: updateResult.Errors
                )
            );
        }

        // Step 6: Map updated coins to CoinMarketData response format
        var coinMarketData = Mapping.ToCoinMarketData(updateResult.Value);

        return Result.Ok(coinMarketData);
    }

    private static class Mapping
    {
        /// <summary>
        /// Maps coins and CoinGecko market data to coin market data update requests.
        /// </summary>
        public static IEnumerable<CoinMarketDataUpdateRequest> ToUpdateRequests(
            IEnumerable<Coin> coins,
            IEnumerable<CoinGeckoAssetInfo> marketData
        )
        {
            var marketDataDictionary = marketData.ToDictionary(data => data.Id, data => data);

            return coins
                .Where(coin =>
                    !string.IsNullOrWhiteSpace(coin.IdCoinGecko)
                    && marketDataDictionary.ContainsKey(coin.IdCoinGecko)
                )
                .Select(coin =>
                {
                    var assetInfo = marketDataDictionary[coin.IdCoinGecko!];
                    return new CoinMarketDataUpdateRequest
                    {
                        Id = coin.Id,
                        MarketCapUsd = assetInfo.MarketCapUsd,
                        PriceUsd = assetInfo.PriceUsd,
                        PriceChangePercentage24h = assetInfo.PriceChangePercentage24h,
                    };
                });
        }

        /// <summary>
        /// Maps updated coins to CoinMarketData response format.
        /// </summary>
        public static IEnumerable<CoinMarketData> ToCoinMarketData(
            IEnumerable<Coin> updatedCoins
        ) =>
            updatedCoins.Select(coin => new CoinMarketData
            {
                Id = coin.Id,
                MarketCapUsd = coin.MarketCapUsd,
                PriceUsd = coin.PriceUsd,
                PriceChangePercentage24h = coin.PriceChangePercentage24h,
            });
    }
}
