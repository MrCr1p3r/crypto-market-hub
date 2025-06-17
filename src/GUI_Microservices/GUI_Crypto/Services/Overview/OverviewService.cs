using FluentResults;
using GUI_Crypto.ApiContracts.Requests.CoinCreation;
using GUI_Crypto.ApiContracts.Responses;
using GUI_Crypto.ApiContracts.Responses.CandidateCoin;
using GUI_Crypto.ApiContracts.Responses.OverviewCoin;
using GUI_Crypto.MicroserviceClients.SvcCoins;
using GUI_Crypto.MicroserviceClients.SvcExternal;
using GUI_Crypto.MicroserviceClients.SvcKline;
using SvcCoins = GUI_Crypto.MicroserviceClients.SvcCoins.Contracts;
using SvcExternal = GUI_Crypto.MicroserviceClients.SvcExternal.Contracts;

namespace GUI_Crypto.Services.Overview;

/// <summary>
/// Service for retrieving and managing overview data.
/// </summary>
public class OverviewService(
    ISvcCoinsClient coinsClient,
    ISvcKlineClient klineClient,
    ISvcExternalClient externalClient
) : IOverviewService
{
    private readonly ISvcCoinsClient _coinsClient = coinsClient;
    private readonly ISvcKlineClient _klineClient = klineClient;
    private readonly ISvcExternalClient _externalClient = externalClient;

    /// <inheritdoc/>
    public async Task<Result<IEnumerable<OverviewCoin>>> GetOverviewCoins()
    {
        var coinsTask = _coinsClient.GetAllCoins();
        var klineTask = _klineClient.GetAllKlineData();
        await Task.WhenAll(coinsTask, klineTask);

        var coinsResult = await coinsTask;
        if (coinsResult.IsFailed)
        {
            return Result.Fail<IEnumerable<OverviewCoin>>(coinsResult.Errors);
        }

        var klineResult = await klineTask;
        if (klineResult.IsFailed)
        {
            return Result.Fail<IEnumerable<OverviewCoin>>(klineResult.Errors);
        }

        var overviewCoins = Mapping.ToOverviewCoins(coinsResult.Value, klineResult.Value);
        return Result.Ok(overviewCoins);
    }

    /// <inheritdoc/>
    public async Task<Result<IEnumerable<CandidateCoin>>> GetCandidateCoins()
    {
        var spotCoinsResultTask = _externalClient.GetAllSpotCoins();
        var dbCoinsResultTask = _coinsClient.GetAllCoins();
        await Task.WhenAll(spotCoinsResultTask, dbCoinsResultTask);

        var spotCoinsResult = await spotCoinsResultTask;
        if (spotCoinsResult.IsFailed)
        {
            return Result.Fail(spotCoinsResult.Errors);
        }

        var dbCoinsResult = await dbCoinsResultTask;
        if (dbCoinsResult.IsFailed)
        {
            return Result.Fail(dbCoinsResult.Errors);
        }

        var candidateCoins = Mapping.ToCandidateCoins(spotCoinsResult.Value, dbCoinsResult.Value);
        return Result.Ok(candidateCoins);
    }

    /// <inheritdoc/>
    public async Task<Result<IEnumerable<OverviewCoin>>> CreateCoins(
        IEnumerable<CoinCreationRequest> requests
    )
    {
        var clientRequests = Mapping.ToSvcCoinsCreationRequests(requests);

        var clientResult = await _coinsClient.CreateCoins(clientRequests);
        if (clientResult.IsFailed)
        {
            return Result.Fail<IEnumerable<OverviewCoin>>(clientResult.Errors);
        }

        var createdCoins = clientResult.Value.Select(Mapping.ToOverviewCoin);
        return Result.Ok(createdCoins);
    }

    /// <inheritdoc/>
    public async Task<Result> DeleteMainCoin(int idCoin)
    {
        return await _coinsClient.DeleteMainCoin(idCoin);
    }

    /// <inheritdoc/>
    public async Task<Result> DeleteAllCoins()
    {
        return await _coinsClient.DeleteAllCoins();
    }

    private static class Mapping
    {
        /// <summary>
        /// Converts a list of coins to a list of overview coins.
        /// </summary>
        public static IEnumerable<OverviewCoin> ToOverviewCoins(
            IEnumerable<SvcCoins.Responses.Coin> coins,
            IEnumerable<MicroserviceClients.SvcKline.Contracts.Responses.KlineDataResponse> allKlineData
        )
        {
            var mainCoins = coins.Where(coin => coin.TradingPairs.Any());

            return mainCoins.Select(coin => ToOverviewCoin(coin, allKlineData));
        }

        private static OverviewCoin ToOverviewCoin(
            SvcCoins.Responses.Coin coin,
            IEnumerable<MicroserviceClients.SvcKline.Contracts.Responses.KlineDataResponse> allKlineData
        )
        {
            // Find the trading pair that has kline data available
            var currentTradingPair = coin.TradingPairs.FirstOrDefault(tradingPair =>
                allKlineData.Any(kline => kline.IdTradingPair == tradingPair.Id)
            );

            KlineData? klineData = null;

            if (currentTradingPair != null)
            {
                var klines = allKlineData
                    .First(response => response.IdTradingPair == currentTradingPair.Id)
                    .KlineData.Select(kline => new Kline
                    {
                        OpenTime = kline.OpenTime,
                        OpenPrice = kline.OpenPrice,
                        HighPrice = kline.HighPrice,
                        LowPrice = kline.LowPrice,
                        ClosePrice = kline.ClosePrice,
                        Volume = kline.Volume,
                        CloseTime = kline.CloseTime,
                    });

                klineData = new KlineData
                {
                    TradingPairId = currentTradingPair.Id,
                    Klines = klines,
                };
            }

            return new OverviewCoin
            {
                Id = coin.Id,
                Symbol = coin.Symbol,
                Name = coin.Name,
                Category = coin.Category,
                MarketCapUsd = coin.MarketCapUsd,
                PriceUsd = coin.PriceUsd,
                PriceChangePercentage24h = coin.PriceChangePercentage24h,
                TradingPairIds = coin.TradingPairs.Select(tp => tp.Id),
                KlineData = klineData,
            };
        }

        /// <summary>
        /// Converts a list of spot coins and database coins to a list of candidate coins.
        /// </summary>
        public static IEnumerable<CandidateCoin> ToCandidateCoins(
            IEnumerable<SvcExternal.Responses.Coins.Coin> spotCoins,
            IEnumerable<SvcCoins.Responses.Coin> dbCoins
        )
        {
            var dbCoinsArray = dbCoins.ToArray();

            var dbMainCoinsSymbolNamePairs = dbCoinsArray
                .Where(coin => coin.TradingPairs.Any())
                .Select(coin => (coin.Symbol, coin.Name));

            var spotCoinsWithoutDbMainCoins = spotCoins.Where(coin =>
                coin.Name != null && !dbMainCoinsSymbolNamePairs.Contains((coin.Symbol, coin.Name!))
            );

            var dbCoinIdBySymbolName = dbCoinsArray.ToDictionary(
                coin => (coin.Symbol, coin.Name!),
                coin => coin.Id
            );

            var candidateCoins = spotCoinsWithoutDbMainCoins.Select(coin => new CandidateCoin
            {
                Id = GetCandidateCoinId(coin.Symbol, coin.Name!, dbCoinIdBySymbolName),
                Symbol = coin.Symbol,
                Name = coin.Name!,
                Category = coin.Category,
                IdCoinGecko = coin.IdCoinGecko,
                TradingPairs = coin.TradingPairs.Select(tp =>
                    ToCandidateCoinTradingPair(tp, dbCoinIdBySymbolName)
                ),
            });

            return candidateCoins;
        }

        private static int? GetCandidateCoinId(
            string symbol,
            string name,
            Dictionary<(string Symbol, string Name), int> dbCoinIdBySymbolName
        ) => dbCoinIdBySymbolName.TryGetValue((symbol, name), out var id) ? id : null;

        private static CandidateCoinTradingPair ToCandidateCoinTradingPair(
            SvcExternal.Responses.Coins.TradingPair tp,
            Dictionary<(string Symbol, string Name), int> dbQuoteCoinIdBySymbolName
        ) =>
            new()
            {
                CoinQuote = new CandidateCoinTradingPairCoinQuote
                {
                    Id = GetCandidateCoinId(
                        tp.CoinQuote.Symbol,
                        tp.CoinQuote.Name!,
                        dbQuoteCoinIdBySymbolName
                    ),
                    Symbol = tp.CoinQuote.Symbol,
                    Name = tp.CoinQuote.Name!,
                    Category = tp.CoinQuote.Category,
                    IdCoinGecko = tp.CoinQuote.IdCoinGecko,
                },
                Exchanges = tp.ExchangeInfos.Select(ei => ei.Exchange),
            };

        /// <summary>
        /// Converts a list of coin creation requests to a list of service client requests.
        /// </summary>
        public static IEnumerable<SvcCoins.Requests.CoinCreation.CoinCreationRequest> ToSvcCoinsCreationRequests(
            IEnumerable<CoinCreationRequest> coins
        ) =>
            coins.Select(coin => new SvcCoins.Requests.CoinCreation.CoinCreationRequest
            {
                Id = coin.Id,
                Symbol = coin.Symbol,
                Name = coin.Name,
                Category = coin.Category,
                IdCoinGecko = coin.IdCoinGecko,
                TradingPairs = coin.TradingPairs.Select(
                    tp => new SvcCoins.Requests.CoinCreation.CoinCreationTradingPair
                    {
                        CoinQuote = new SvcCoins.Requests.CoinCreation.CoinCreationCoinQuote
                        {
                            Id = tp.CoinQuote.Id,
                            Symbol = tp.CoinQuote.Symbol,
                            Name = tp.CoinQuote.Name,
                            Category = tp.CoinQuote.Category,
                            IdCoinGecko = tp.CoinQuote.IdCoinGecko,
                        },
                        Exchanges = tp.Exchanges,
                    }
                ),
            });

        /// <summary>
        /// Converts a coin to an overview coin.
        /// </summary>
        public static OverviewCoin ToOverviewCoin(SvcCoins.Responses.Coin coin) =>
            new()
            {
                Id = coin.Id,
                Symbol = coin.Symbol,
                Name = coin.Name,
                Category = coin.Category,
                MarketCapUsd = coin.MarketCapUsd,
                PriceUsd = coin.PriceUsd,
                PriceChangePercentage24h = coin.PriceChangePercentage24h,
                TradingPairIds = coin.TradingPairs.Select(tp => tp.Id),
            };
    }
}
