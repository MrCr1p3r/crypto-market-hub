using System.Collections.Frozen;
using FluentResults;
using ISO._4217;
using Microsoft.Extensions.Caching.Hybrid;
using SharedLibrary.Enums;
using SharedLibrary.Exceptions;
using SVC_External.ApiContracts.Responses.Exchanges.Coins;
using SVC_External.ExternalClients.Exchanges;
using SVC_External.ExternalClients.Exchanges.Contracts.Responses;
using SVC_External.ExternalClients.MarketDataProviders.CoinGecko;
using SVC_External.ExternalClients.MarketDataProviders.CoinGecko.Contracts.Responses;
using SVC_External.Services.Exchanges.Interfaces;
using SVC_External.Services.Exchanges.Logging;
using static SharedLibrary.Errors.GenericErrors;

namespace SVC_External.Services.Exchanges;

/// <summary>
/// Implementation of the coins service that fetches coin data from multiple exchange clients.
/// </summary>
public class CoinsService(
    IEnumerable<IExchangesClient> exchangeClients,
    ICoinGeckoClient coinGeckoClient,
    HybridCache hybridCache,
    ILogger<CoinsService> logger
) : ICoinsService
{
    private readonly IEnumerable<IExchangesClient> _exchangeClients = exchangeClients;
    private readonly ICoinGeckoClient _coinGeckoClient = coinGeckoClient;
    private readonly HybridCache _hybridCache = hybridCache;
    private readonly ILogger<CoinsService> _logger = logger;

    /// <inheritdoc />
    public async Task<Result<IEnumerable<Coin>>> GetAllCurrentActiveSpotCoins()
    {
        var cacheKey = "all_current_active_spot_coins";
        try
        {
            var result = await _hybridCache.GetOrCreateAsync(
                cacheKey,
                async _ =>
                {
                    var coinsResult = await GetAllActiveSpotCoins();
                    return coinsResult.IsSuccess
                        ? coinsResult
                        : throw new CacheBypassException<IEnumerable<Coin>>(coinsResult);
                }
            );
            return result;
        }
        catch (CacheBypassException<IEnumerable<Coin>> exception)
        {
            return exception.Result;
        }
    }

    private async Task<Result<IEnumerable<Coin>>> GetAllActiveSpotCoins()
    {
        var coinsLists = await Task.WhenAll(
            _exchangeClients.Select(client => client.GetAllSpotCoins())
        );
        if (coinsLists.Any(list => list.IsFailed))
        {
            var failedResult = Result.Fail(
                new InternalError(
                    $"No coins found for one or more exchanges. See reasons for more information.",
                    reasons: coinsLists.Where(list => list.IsFailed).SelectMany(list => list.Errors)
                )
            );
            return failedResult;
        }

        var convertedCoinsLists = coinsLists.Select(coinsList =>
            coinsList.Value.Select(Mapping.ToCoin)
        );
        var activeCoinsLists = ExcludeInactiveCoins(convertedCoinsLists);

        var geckoCoinsResult = await _coinGeckoClient.GetCoinsList();
        if (geckoCoinsResult.IsFailed)
        {
            return Result.Fail(
                new InternalError(
                    $"Failed to retrieve a coins list from CoinGecko.",
                    reasons: geckoCoinsResult.Errors
                )
            );
        }

        var processExchangeCoinsResults = await Task.WhenAll(
            activeCoinsLists.Select(async exchangeCoins =>
                (
                    Coins: exchangeCoins,
                    Result: await ProcessExchangeCoins(exchangeCoins, geckoCoinsResult.Value)
                )
            )
        );
        if (processExchangeCoinsResults.Any(combinedResult => combinedResult.Result.IsFailed))
        {
            return Result.Fail(
                new InternalError(
                    $"Failed to process exchange coins.",
                    reasons: processExchangeCoinsResults
                        .Where(combinedResult => combinedResult.Result.IsFailed)
                        .SelectMany(combinedResult => combinedResult.Result.Errors)
                )
            );
        }

        var processedCoins = processExchangeCoinsResults
            .Select(combinedResult => combinedResult.Coins)
            .SelectMany(coins => coins);

        return Result.Ok(GroupCoinsBySymbol(processedCoins));
    }

    private static List<List<Coin>> ExcludeInactiveCoins(
        IEnumerable<IEnumerable<Coin>> coinsLists
    ) =>
        [
            .. coinsLists.Select(coinsList =>
                coinsList
                    .Where(coin =>
                        coin.TradingPairs.Any(tradingPair =>
                            tradingPair.ExchangeInfos.Any(info =>
                                info.Status == ExchangeTradingPairStatus.Available
                            )
                        )
                    )
                    .Select(coin =>
                    {
                        coin.TradingPairs =
                        [
                            .. coin.TradingPairs.Where(tradingPair =>
                                tradingPair.ExchangeInfos.Any(info =>
                                    info.Status == ExchangeTradingPairStatus.Available
                                )
                            ),
                        ];
                        return coin;
                    })
                    .ToList()
            ),
        ];

    private async Task<Result> ProcessExchangeCoins(
        List<Coin> exchangeCoins,
        IEnumerable<CoinCoinGecko> geckoCoins
    )
    {
        string idExchange = GetIdExchange(exchangeCoins);
        var symbolToIdMap = await _coinGeckoClient.GetSymbolToIdMapForExchange(idExchange);
        if (symbolToIdMap.IsFailed)
        {
            return Result.Fail(
                new InternalError(
                    $"Failed to retrieve a symbol to ID map for exchange: {idExchange}.",
                    reasons: symbolToIdMap.Errors
                )
            );
        }

        var quoteCoins = exchangeCoins
            .SelectMany(coin => coin.TradingPairs)
            .Select(tradingPair => tradingPair.CoinQuote)
            .ToList();

        UpdateCoins(exchangeCoins, symbolToIdMap.Value, geckoCoins, idExchange);
        UpdateCoins(quoteCoins, symbolToIdMap.Value, geckoCoins, idExchange);
        return Result.Ok();
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
        {
            return;
        }

        UpdateCoinsFromIsoCodes(remainingCoins, updatedCoins);

        var coinsWithoutNames = coinsToUpdate
            .Except(updatedCoins)
            .DistinctBy(coin => coin.Symbol)
            .ToArray();
        if (coinsWithoutNames.Length == 0)
        {
            return;
        }

        if (typeof(T) == typeof(Coin))
        {
            _logger.LogSymbolsWithoutNames(
                idExchange,
                string.Join(", ", coinsWithoutNames.Select(coin => coin.Symbol))
            );
        }
        else
        {
            _logger.LogQuoteSymbolsWithoutNames(
                idExchange,
                string.Join(", ", coinsWithoutNames.Select(coin => coin.Symbol))
            );
        }
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
            {
                continue;
            }

            var geckoCoin = geckoCoins.FirstOrDefault(geckoCoin => geckoCoin.Id == id);
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
        {
            _logger.LogInactiveCoinGeckoCoins(
                idExchange,
                string.Join(
                    ", ",
                    inactiveCoins.Select(coin =>
                    {
                        symbolToIdMap.TryGetValue(coin.Symbol, out var id);
                        return $"{coin.Symbol} (coinGeckoId:{id})";
                    })
                )
            );
        }

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
            {
                continue;
            }

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
                        ExchangeInfos = group.SelectMany(tradingPair => tradingPair.ExchangeInfos),
                    }),
            });

        return groupedCoins;
    }

    private static class Mapping
    {
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
    }
}
