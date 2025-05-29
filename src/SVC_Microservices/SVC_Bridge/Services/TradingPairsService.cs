using FluentResults;
using SVC_Bridge.ApiContracts.Responses.Coins;
using SVC_Bridge.MicroserviceClients.SvcCoins;
using SVC_Bridge.MicroserviceClients.SvcCoins.Contracts.Requests;
using SVC_Bridge.MicroserviceClients.SvcExternal;
using SVC_Bridge.Services.Interfaces;
using static SharedLibrary.Errors.GenericErrors;
using SvcCoins = SVC_Bridge.MicroserviceClients.SvcCoins.Contracts.Responses;
using SvcExternal = SVC_Bridge.MicroserviceClients.SvcExternal.Contracts.Responses.Coins;

namespace SVC_Bridge.Services;

/// <summary>
/// Service for managing trading pairs operations.
/// </summary>
public class TradingPairsService(
    ISvcCoinsClient svcCoinsClient,
    ISvcExternalClient svcExternalClient
) : ITradingPairsService
{
    private readonly ISvcCoinsClient _svcCoinsClient = svcCoinsClient;
    private readonly ISvcExternalClient _svcExternalClient = svcExternalClient;

    /// <inheritdoc />
    public async Task<Result<IEnumerable<Coin>>> UpdateTradingPairs()
    {
        // Step 1: Retrieve coins from SVC_Coins and spot coins from SVC_External in parallel
        var coinsResultTask = _svcCoinsClient.GetAllCoins();
        var spotCoinsResultTask = _svcExternalClient.GetAllSpotCoins();
        await Task.WhenAll(coinsResultTask, spotCoinsResultTask);

        // Step 2: Validate the result of the coins retrieval from SVC_Coins
        var coinsResult = await coinsResultTask;
        if (coinsResult.IsFailed)
        {
            return Result.Fail(
                new InternalError(
                    "Failed to retrieve coins from coins service.",
                    reasons: coinsResult.Errors
                )
            );
        }

        // Step 3: Validate the result of the spot coins retrieval from SVC_External
        var spotCoinsResult = await spotCoinsResultTask;
        if (spotCoinsResult.IsFailed)
        {
            return Result.Fail(
                new InternalError(
                    "Failed to retrieve spot coins from external service.",
                    reasons: spotCoinsResult.Errors
                )
            );
        }

        // Step 4: Filter spot coins to include only those matching existing main coins
        var dbCoins = coinsResult.Value;
        var spotCoins = spotCoinsResult.Value;
        var validSpotCoins = Mapping.GetValidSpotCoins(dbCoins, spotCoins);
        if (!validSpotCoins.Any())
        {
            return Result.Ok(Enumerable.Empty<Coin>());
        }

        // Step 5: Identify and create new quote coins that don't exist in the database
        var newQuoteCoins = Mapping.GetNewQuoteCoins(dbCoins, validSpotCoins);
        var createdQuoteCoins = new List<SvcCoins.Coin>();
        if (newQuoteCoins.Any())
        {
            var coinCreationRequests = newQuoteCoins.Select(Mapping.ToQuoteCoinCreationRequest);
            var createCoinsResult = await _svcCoinsClient.CreateQuoteCoins(coinCreationRequests);
            if (createCoinsResult.IsFailed)
            {
                return Result.Fail(
                    new InternalError(
                        "Failed to create new quote coins.",
                        reasons: createCoinsResult.Errors
                    )
                );
            }

            createdQuoteCoins = [.. createCoinsResult.Value.Select(Mapping.ToSvcCoinsCoin)];
        }

        // Update the coins collection with newly created quote coins
        dbCoins = dbCoins.Concat(createdQuoteCoins);

        // Step 6: Generate trading pair creation requests from valid spot coins and available coins
        var tradingPairCreationRequests = Mapping.ToTradingPairCreationRequests(
            validSpotCoins,
            dbCoins
        );
        if (tradingPairCreationRequests.Count == 0)
        {
            return Result.Ok(Enumerable.Empty<Coin>());
        }

        // Step 7: Replace all existing trading pairs with the new ones
        var replaceTradingPairsResult = await _svcCoinsClient.ReplaceTradingPairs(
            tradingPairCreationRequests
        );
        if (replaceTradingPairsResult.IsFailed)
        {
            return Result.Fail(
                new InternalError(
                    "Failed to replace trading pairs.",
                    reasons: replaceTradingPairsResult.Errors
                )
            );
        }

        // Step 8: Clean up coins that are no longer referenced by any trading pairs
        var deleteUnreferencedCoinsResult = await _svcCoinsClient.DeleteUnreferencedCoins();
        if (deleteUnreferencedCoinsResult.IsFailed)
        {
            return Result.Fail(
                new InternalError(
                    "Failed to delete unreferenced coins.",
                    reasons: deleteUnreferencedCoinsResult.Errors
                )
            );
        }

        // Step 9: Transform the updated coins to API response format and return them
        return Result.Ok(Mapping.ToApiResponseCoins(replaceTradingPairsResult.Value));
    }

    private static class Mapping
    {
        /// <summary>
        /// Filters spot coins to include only those that match existing main coins in the database.
        /// Main coins are defined as coins that already have at least one trading pair.
        /// Matching is performed using both symbol and name for accuracy.
        /// </summary>
        public static IEnumerable<SvcExternal.Coin> GetValidSpotCoins(
            IEnumerable<SvcCoins.Coin> dbCoins,
            IEnumerable<SvcExternal.Coin> spotCoins
        )
        {
            var dbMainCoins = dbCoins.Where(coin => coin.TradingPairs.Any());
            var dbMainCoinsBySymbolName = dbMainCoins.ToDictionary(
                coin => (coin.Symbol, coin.Name),
                coin => coin
            );

            var validSpotCoins = spotCoins.Where(spotCoin =>
                spotCoin.Name != null
                && dbMainCoinsBySymbolName.ContainsKey((spotCoin.Symbol, spotCoin.Name))
            );
            return validSpotCoins;
        }

        /// <summary>
        /// Identifies quote coins from spot trading pairs that don't exist in the database.
        /// </summary>
        public static IEnumerable<SvcExternal.TradingPairCoinQuote> GetNewQuoteCoins(
            IEnumerable<SvcCoins.Coin> dbCoins,
            IEnumerable<SvcExternal.Coin> validSpotCoins
        )
        {
            var spotQuoteCoins = validSpotCoins
                .SelectMany(spotCoin => spotCoin.TradingPairs)
                .Select(tradingPair => tradingPair.CoinQuote);
            var newQuoteCoins = spotQuoteCoins
                .Where(spotQuoteCoin =>
                    !dbCoins.Any(dbQuoteCoin =>
                        dbQuoteCoin.Symbol == spotQuoteCoin.Symbol
                        && dbQuoteCoin.Name == spotQuoteCoin.Name
                    )
                )
                .Distinct();

            return newQuoteCoins;
        }

        /// <summary>
        /// Converts a quote coin from external service format to a quote coin creation request.
        /// </summary>
        public static QuoteCoinCreationRequest ToQuoteCoinCreationRequest(
            SvcExternal.TradingPairCoinQuote quoteCoin
        ) =>
            new()
            {
                Symbol = quoteCoin.Symbol,
                Name = quoteCoin.Name!,
                Category = quoteCoin.Category,
                IdCoinGecko = quoteCoin.IdCoinGecko,
            };

        /// <summary>
        /// Converts a trading pair coin quote response to a standard coin format.
        /// </summary>
        public static SvcCoins.Coin ToSvcCoinsCoin(SvcCoins.TradingPairCoinQuote quoteCoin) =>
            new()
            {
                Id = quoteCoin.Id,
                Symbol = quoteCoin.Symbol,
                Name = quoteCoin.Name!,
                Category = quoteCoin.Category,
                IdCoinGecko = quoteCoin.IdCoinGecko,
                MarketCapUsd = quoteCoin.MarketCapUsd,
                PriceUsd = quoteCoin.PriceUsd,
                PriceChangePercentage24h = quoteCoin.PriceChangePercentage24h,
            };

        /// <summary>
        /// Creates trading pair creation requests by mapping spot coins to database coin IDs.
        /// Each trading pair request includes the main coin ID, quote coin ID, and available exchanges.
        /// </summary>
        public static List<TradingPairCreationRequest> ToTradingPairCreationRequests(
            IEnumerable<SvcExternal.Coin> spotCoins,
            IEnumerable<SvcCoins.Coin> dbCoins
        )
        {
            var dbCoinIdsBySymbolName = dbCoins.ToDictionary(
                coin => (coin.Symbol, coin.Name),
                coin => coin.Id
            );

            var tradingPairCreationRequests = spotCoins.SelectMany(spotCoin =>
                spotCoin.TradingPairs.Select(tradingPair => new TradingPairCreationRequest
                {
                    IdCoinMain = dbCoinIdsBySymbolName[(spotCoin.Symbol, spotCoin.Name!)],
                    IdCoinQuote = dbCoinIdsBySymbolName[
                        (tradingPair.CoinQuote.Symbol, tradingPair.CoinQuote.Name!)
                    ],
                    Exchanges = tradingPair.ExchangeInfos.Select(exchangeInfo =>
                        exchangeInfo.Exchange
                    ),
                })
            );

            return [.. tradingPairCreationRequests];
        }

        /// <summary>
        /// Transforms coins from SVC_Coins service format to API response format.
        /// This includes converting all nested trading pairs and quote coin information.
        /// </summary>
        public static IEnumerable<Coin> ToApiResponseCoins(IEnumerable<SvcCoins.Coin> svcCoins) =>
            svcCoins.Select(coin => new Coin
            {
                Id = coin.Id,
                Name = coin.Name,
                Symbol = coin.Symbol,
                Category = coin.Category,
                IdCoinGecko = coin.IdCoinGecko,
                MarketCapUsd = coin.MarketCapUsd,
                PriceUsd = coin.PriceUsd,
                PriceChangePercentage24h = coin.PriceChangePercentage24h,
                TradingPairs = coin.TradingPairs.Select(tp => new TradingPair
                {
                    Id = tp.Id,
                    CoinQuote = new TradingPairCoinQuote
                    {
                        Id = tp.CoinQuote.Id,
                        Name = tp.CoinQuote.Name,
                        Symbol = tp.CoinQuote.Symbol,
                        Category = tp.CoinQuote.Category,
                        IdCoinGecko = tp.CoinQuote.IdCoinGecko,
                        MarketCapUsd = tp.CoinQuote.MarketCapUsd,
                        PriceUsd = tp.CoinQuote.PriceUsd,
                        PriceChangePercentage24h = tp.CoinQuote.PriceChangePercentage24h,
                    },
                    Exchanges = tp.Exchanges,
                }),
            });
    }
}
