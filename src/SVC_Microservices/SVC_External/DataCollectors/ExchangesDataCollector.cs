using System.Collections.Frozen;
using ISO._4217;
using Microsoft.Extensions.Caching.Hybrid;
using SharedLibrary.Enums;
using SVC_External.Clients.Exchanges.Interfaces;
using SVC_External.Clients.MarketDataProviders.Interfaces;
using SVC_External.DataCollectors.Interfaces;
using SVC_External.DataCollectors.Logging;
using SVC_External.Models.Exchanges.Input;
using SVC_External.Models.Exchanges.Output;
using SVC_External.Models.Input;
using SVC_External.Models.MarketDataProviders.Output;
using SVC_External.Models.Output;

namespace SVC_External.DataCollectors;

/// <summary>
/// Implementation of the data collector that fetches the data from multiple exchange clients.
/// </summary>
public class ExchangesDataCollector(
    IEnumerable<IExchangesClient> exchangeClients,
    ICoinGeckoClient coinGeckoClient,
    HybridCache hybridCache,
    ILogger<ExchangesDataCollector> logger
) : IExchangesDataCollector
{
    private readonly IEnumerable<IExchangesClient> _exchangeClients = exchangeClients;
    private readonly ICoinGeckoClient _coinGeckoClient = coinGeckoClient;
    private readonly HybridCache _hybridCache = hybridCache;
    private readonly ILogger<ExchangesDataCollector> _logger = logger;

    #region GetAllCurrentActiveSpotCoins
    /// <inheritdoc />
    public async Task<IEnumerable<Coin>> GetAllCurrentActiveSpotCoins()
    {
        var cacheKey = "all_current_active_spot_coins";
        return await _hybridCache.GetOrCreateAsync(
            cacheKey,
            async _ => await GetAllActiveSpotCoins()
        );
    }

    private async Task<IEnumerable<Coin>> GetAllActiveSpotCoins()
    {
        var coinsLists = await Task.WhenAll(
            _exchangeClients.Select(client => client.GetAllSpotCoins())
        );
        if (coinsLists.Any(list => !list.Any()))
            return [];

        var convertedCoinsLists = coinsLists.Select(coinsList => coinsList.Select(Mapping.ToCoin));
        var activeCoinsLists = ExcludeInactiveCoins(convertedCoinsLists);

        var geckoCoins = await _coinGeckoClient.GetCoinsList();
        if (!geckoCoins.Any())
            return [];

        var processExchangeCoinsResults = await Task.WhenAll(
            activeCoinsLists.Select(async exchangeCoins =>
                (
                    Coins: exchangeCoins,
                    Processed: await ProcessExchangeCoins(exchangeCoins, geckoCoins)
                )
            )
        );
        if (processExchangeCoinsResults.Any(result => !result.Processed))
            return [];

        var processedCoinsLists = processExchangeCoinsResults
            .Where(result => result.Processed)
            .Select(result => result.Coins);

        return GroupCoinsBySymbol(processedCoinsLists.SelectMany(coins => coins));
    }

    private static List<List<Coin>> ExcludeInactiveCoins(
        IEnumerable<IEnumerable<Coin>> coinsLists
    ) =>
        [
            .. coinsLists.Select(coinsList =>
                coinsList
                    .Where(coin =>
                        coin.TradingPairs.Any(tp =>
                            tp.ExchangeInfos.Any(info =>
                                info.Status == ExchangeTradingPairStatus.Available
                            )
                        )
                    )
                    .Select(coin =>
                    {
                        coin.TradingPairs =
                        [
                            .. coin.TradingPairs.Where(tp =>
                                tp.ExchangeInfos.Any(info =>
                                    info.Status == ExchangeTradingPairStatus.Available
                                )
                            ),
                        ];
                        return coin;
                    })
                    .ToList()
            ),
        ];

    private async Task<bool> ProcessExchangeCoins(
        List<Coin> exchangeCoins,
        IEnumerable<CoinCoinGecko> geckoCoins
    )
    {
        string idExchange = GetIdExchange(exchangeCoins);
        var symbolToIdMap = await _coinGeckoClient.GetSymbolToIdMapForExchange(idExchange);
        if (symbolToIdMap.Count == 0)
            return false;

        var quoteCoins = exchangeCoins
            .SelectMany(coin => coin.TradingPairs)
            .Select(tp => tp.CoinQuote)
            .ToList();

        UpdateCoins(exchangeCoins, symbolToIdMap, geckoCoins, idExchange);
        UpdateCoins(quoteCoins, symbolToIdMap, geckoCoins, idExchange);
        return true;
    }

    private static string GetIdExchange(IEnumerable<Coin> exchangeCoins) =>
        exchangeCoins.First().TradingPairs.First().ExchangeInfos.First().Exchange switch
        {
            Exchange.Binance => "binance",
            Exchange.Mexc => "mxc",
            Exchange.Bybit => "bybit_spot",
            _ => throw new InvalidOperationException("Unsupported exchange"),
        };

    private void UpdateCoins<T>(
        List<T> coinsToUpdate,
        FrozenDictionary<string, string?> symbolToIdMap,
        IEnumerable<CoinCoinGecko> geckoCoins,
        string idExchange
    )
        where T : CoinBase
    {
        var updatedCoins = UpdateCoinsFromGecko(
            coinsToUpdate,
            symbolToIdMap,
            geckoCoins,
            idExchange
        );

        var remainingCoins = coinsToUpdate.Except(updatedCoins);
        if (!remainingCoins.Any())
            return;

        UpdateCoinsFromIsoCodes(remainingCoins, updatedCoins);

        var coinsWithoutNames = coinsToUpdate
            .Except(updatedCoins)
            .DistinctBy(c => c.Symbol)
            .ToArray();
        if (coinsWithoutNames.Length == 0)
            return;

        if (typeof(T) == typeof(Coin))
            _logger.LogSymbolsWithoutNames(
                idExchange,
                string.Join(", ", coinsWithoutNames.Select(c => c.Symbol))
            );
        else
            _logger.LogQuoteSymbolsWithoutNames(
                idExchange,
                string.Join(", ", coinsWithoutNames.Select(c => c.Symbol))
            );
    }

    private List<T> UpdateCoinsFromGecko<T>(
        List<T> coinsToUpdate,
        FrozenDictionary<string, string?> symbolToIdMap,
        IEnumerable<CoinCoinGecko> geckoCoins,
        string idExchange
    )
        where T : CoinBase
    {
        var updatedCoins = new List<T>();
        var inactiveCoins = new List<T>();

        foreach (var coin in coinsToUpdate)
        {
            if (!symbolToIdMap.TryGetValue(coin.Symbol, out var id) || id is null)
                continue;

            var geckoCoin = geckoCoins.FirstOrDefault(gc => gc.Id == id);
            if (geckoCoin is null)
            {
                inactiveCoins.Add(coin);
                updatedCoins.Add(coin);
                continue;
            }

            coin.Name = geckoCoin.Name;
            coin.IdCoinGecko = geckoCoin.Id;
            updatedCoins.Add(coin);
        }

        if (inactiveCoins.Count != 0)
            _logger.LogInactiveCoinGeckoCoins(
                idExchange,
                string.Join(
                    ", ",
                    inactiveCoins.Select(c =>
                    {
                        symbolToIdMap.TryGetValue(c.Symbol, out var id);
                        return $"{c.Symbol} (coinGeckoId:{id})";
                    })
                )
            );

        return updatedCoins;
    }

    private static void UpdateCoinsFromIsoCodes<T>(
        IEnumerable<T> coinsToUpdate,
        List<T> updatedCoins
    )
        where T : CoinBase
    {
        foreach (var coin in coinsToUpdate)
        {
            var currencies = CurrencyCodesResolver.GetCurrenciesByCode(coin.Symbol);
            if (currencies?.Any() == false)
                continue;

            coin.Name = currencies!.First().Name;
            coin.Category = CoinCategory.Fiat;
            updatedCoins.Add(coin);
        }
    }

    private static IEnumerable<Coin> GroupCoinsBySymbol(IEnumerable<Coin> coins)
    {
        var groupedCoins = coins
            .GroupBy(coin => new { coin.Symbol, coin.Name })
            .Select(group => new Coin
            {
                Symbol = group.Key.Symbol,
                Name = group.Key.Name,
                IdCoinGecko = group
                    .FirstOrDefault(coin => coin.IdCoinGecko is not null)
                    ?.IdCoinGecko,
                Category = group.First().Category,
                TradingPairs = group
                    .SelectMany(coin => coin.TradingPairs)
                    .GroupBy(tradingPair => new
                    {
                        tradingPair.CoinQuote.Symbol,
                        tradingPair.CoinQuote.Name,
                    })
                    .Select(group => new TradingPair
                    {
                        CoinQuote = new TradingPairCoinQuote
                        {
                            Symbol = group.Key.Symbol,
                            Name = group.Key.Name,
                            Category = group.First().CoinQuote.Category,
                            IdCoinGecko = group
                                .FirstOrDefault(coin => coin.CoinQuote.IdCoinGecko is not null)
                                ?.CoinQuote.IdCoinGecko,
                        },
                        ExchangeInfos = group.SelectMany(tp => tp.ExchangeInfos),
                    }),
            });

        return groupedCoins;
    }
    #endregion

    #region GetKlineData
    /// <inheritdoc />
    public async Task<KlineDataRequestResponse> GetKlineData(KlineDataRequest request)
    {
        var suitableClients = PickExchangesClientsForTradingPair(request.TradingPair);
        var formattedRequest = Mapping.ToFormattedRequest(request);
        var klineData = await GetKlineDataForTradingPair(suitableClients, formattedRequest);
        if (!klineData.Any())
        {
            _logger.LogNoKlineDataFoundForCoin(
                request.CoinMain.Id,
                request.CoinMain.Symbol,
                request.CoinMain.Name
            );
            return new KlineDataRequestResponse();
        }
        return Mapping.ToOutputKlineDataRequestResponse(request.TradingPair.Id, klineData);
    }

    private IEnumerable<IExchangesClient> PickExchangesClientsForTradingPair(
        KlineDataRequestTradingPair tradingPair
    ) =>
        tradingPair.Exchanges.Select(exchange =>
        {
            return _exchangeClients.First(client => client.CurrentExchange == exchange);
        });
    #endregion

    #region GetKlineDataBatch
    /// <inheritdoc />
    public async Task<IEnumerable<KlineDataRequestResponse>> GetKlineDataBatch(
        KlineDataBatchRequest request
    )
    {
        var coinTasks = request.MainCoins.Select(mainCoin =>
            ProcessKlineDataRequest(request, mainCoin)
        );
        var results = await Task.WhenAll(coinTasks);

        return results.Where(response => response != null).Cast<KlineDataRequestResponse>();
    }

    private async Task<KlineDataRequestResponse?> ProcessKlineDataRequest(
        KlineDataBatchRequest request,
        KlineDataRequestCoinMain mainCoin
    )
    {
        foreach (var tradingPair in mainCoin.TradingPairs)
        {
            var suitableClients = PickExchangesClientsForTradingPair(tradingPair);
            var formattedRequest = Mapping.ToFormattedRequest(
                mainCoin.Symbol,
                tradingPair.CoinQuote.Symbol,
                request
            );
            var klineData = await GetKlineDataForTradingPair(suitableClients, formattedRequest);
            if (klineData.Any())
            {
                return Mapping.ToOutputKlineDataRequestResponse(tradingPair.Id, klineData);
            }
        }
        _logger.LogNoKlineDataFoundForCoin(mainCoin.Id, mainCoin.Symbol, mainCoin.Name);
        return null;
    }

    private static async Task<IEnumerable<ExchangeKlineData>> GetKlineDataForTradingPair(
        IEnumerable<IExchangesClient> suitableClients,
        ExchangeKlineDataRequest formattedRequest
    )
    {
        foreach (var client in suitableClients)
        {
            var result = await client.GetKlineData(formattedRequest);
            if (result.Any())
            {
                return result;
            }
        }
        return [];
    }
    #endregion

    public async Task<IEnumerable<CoinGeckoAssetInfo>> GetCoinGeckoAssetsInfo(
        IEnumerable<string> ids
    )
    {
        var stablecoinInfosTask = _coinGeckoClient.GetCoinsMarkets(ids);
        var stablecoinIdsTask = _coinGeckoClient.GetStablecoinsIds();
        await Task.WhenAll(stablecoinInfosTask, stablecoinIdsTask);

        var stablecoinInfos = await stablecoinInfosTask;
        var stablecoinIds = await stablecoinIdsTask;
        return stablecoinInfos.Any() && stablecoinIds.Any()
            ? stablecoinInfos.Select(info => Mapping.ToCoinGeckoAssetInfo(info, stablecoinIds))
            : [];
    }

    private static class Mapping
    {
        #region GetAllCurrentActiveSpotCoins
        public static Coin ToCoin(ExchangeCoin exchangeCoin) =>
            new()
            {
                Symbol = exchangeCoin.Symbol,
                Name = exchangeCoin.Name,
                TradingPairs = exchangeCoin.TradingPairs.Select(ToTradingPair),
            };

        public static TradingPair ToTradingPair(ExchangeTradingPair exchangeTradingPair) =>
            new()
            {
                CoinQuote = ToCoinQuote(exchangeTradingPair.CoinQuote),
                ExchangeInfos = [ToTradingPairExchangeInfo(exchangeTradingPair.ExchangeInfo)],
            };

        public static TradingPairCoinQuote ToCoinQuote(
            ExchangeTradingPairCoinQuote exchangeTradingPairCoinQuote
        ) =>
            new()
            {
                Symbol = exchangeTradingPairCoinQuote.Symbol,
                Name = exchangeTradingPairCoinQuote.Name,
            };

        public static TradingPairExchangeInfo ToTradingPairExchangeInfo(
            ExchangeTradingPairExchangeInfo exchangeTradingPairExchangeInfo
        ) =>
            new()
            {
                Exchange = exchangeTradingPairExchangeInfo.Exchange,
                Status = exchangeTradingPairExchangeInfo.Status,
            };
        #endregion

        #region GetKlineData
        public static ExchangeKlineDataRequest ToFormattedRequest(KlineDataRequest request) =>
            new()
            {
                CoinMainSymbol = request.CoinMain.Symbol,
                CoinQuoteSymbol = request.TradingPair.CoinQuote.Symbol,
                Interval = request.Interval,
                StartTimeUnix = new DateTimeOffset(request.StartTime).ToUnixTimeMilliseconds(),
                EndTimeUnix = new DateTimeOffset(request.EndTime).ToUnixTimeMilliseconds(),
                Limit = request.Limit,
            };

        public static ExchangeKlineDataRequest ToFormattedRequest(
            string mainCoinSymbol,
            string quoteCoinSymbol,
            KlineDataBatchRequest request
        ) =>
            new()
            {
                CoinMainSymbol = mainCoinSymbol,
                CoinQuoteSymbol = quoteCoinSymbol,
                Interval = request.Interval,
                StartTimeUnix = new DateTimeOffset(request.StartTime).ToUnixTimeMilliseconds(),
                EndTimeUnix = new DateTimeOffset(request.EndTime).ToUnixTimeMilliseconds(),
                Limit = request.Limit,
            };

        public static KlineDataRequestResponse ToOutputKlineDataRequestResponse(
            int idTradingPair,
            IEnumerable<ExchangeKlineData> klineData
        ) =>
            new()
            {
                IdTradingPair = idTradingPair,
                KlineData = klineData.Select(ToOutputKlineData),
            };

        private static KlineData ToOutputKlineData(ExchangeKlineData klineData) =>
            new()
            {
                OpenTime = klineData.OpenTime,
                OpenPrice = klineData.OpenPrice,
                HighPrice = klineData.HighPrice,
                LowPrice = klineData.LowPrice,
                ClosePrice = klineData.ClosePrice,
                Volume = klineData.Volume,
                CloseTime = klineData.CloseTime,
            };
        #endregion

        #region GetCoinGeckoAssetsInfo
        public static CoinGeckoAssetInfo ToCoinGeckoAssetInfo(
            AssetCoinGecko coinGeckoAssetInfo,
            IEnumerable<string> stablecoinIds
        ) =>
            new()
            {
                Id = coinGeckoAssetInfo.Id,
                MarketCapUsd = coinGeckoAssetInfo.MarketCapUsd,
                PriceUsd = coinGeckoAssetInfo.PriceUsd,
                PriceChangePercentage24h = coinGeckoAssetInfo.PriceChangePercentage24h,
                IsStablecoin = stablecoinIds.Contains(coinGeckoAssetInfo.Id),
            };
        #endregion
    }
}
