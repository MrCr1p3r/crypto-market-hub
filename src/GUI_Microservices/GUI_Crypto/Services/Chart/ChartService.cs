using FluentResults;
using GUI_Crypto.ApiContracts.Requests.KlineData;
using GUI_Crypto.MicroserviceClients.SvcCoins;
using GUI_Crypto.MicroserviceClients.SvcCoins.Contracts.Responses;
using GUI_Crypto.MicroserviceClients.SvcExternal;
using GUI_Crypto.ServiceModels;
using SharedLibrary.Enums;
using SharedLibrary.Models;
using static SharedLibrary.Errors.GenericErrors;
using SvcExternal = GUI_Crypto.MicroserviceClients.SvcExternal.Contracts;

namespace GUI_Crypto.Services.Chart;

/// <summary>
/// Service for handling chart-related operations.
/// </summary>
public class ChartService(ISvcCoinsClient coinsClient, ISvcExternalClient externalClient)
    : IChartService
{
    public const ExchangeKlineInterval DefaultInterval = ExchangeKlineInterval.OneDay;
    public const int DefaultLimit = 1000;
    public static readonly DateTime DefaultStartTime = DateTime.UtcNow.AddDays(-30);
    public static readonly DateTime DefaultEndTime = DateTime.UtcNow;
    private readonly ISvcCoinsClient _coinsClient = coinsClient;
    private readonly ISvcExternalClient _externalClient = externalClient;

    /// <inheritdoc/>
    public async Task<Result<ChartData>> GetChartData(int idCoin, int idTradingPair)
    {
        var coinResult = await _coinsClient.GetCoinById(idCoin);
        if (coinResult.IsFailed)
        {
            return Result.Fail(coinResult.Errors);
        }

        var coin = coinResult.Value;
        var currentTradingPair = coin.TradingPairs.FirstOrDefault(tp => tp.Id == idTradingPair);
        if (currentTradingPair == null)
        {
            return Result.Fail(
                new NotFoundError(
                    $"Trading pair with ID {idTradingPair} was not found for coin {coin.Name} ({coin.Symbol}) with ID {idCoin}."
                )
            );
        }

        var klineRequest = Mapping.ToDefaultKlineDataRequest(coin, currentTradingPair);
        var klineResult = await _externalClient.GetKlineData(klineRequest);
        if (klineResult.IsFailed)
        {
            return Result.Fail(klineResult.Errors);
        }

        var klineData = klineResult.Value;
        var chartData = new ChartData { Coin = coin, KlineResponse = klineData };
        return Result.Ok(chartData);
    }

    /// <inheritdoc/>
    public async Task<Result<IEnumerable<Kline>>> GetKlineData(KlineDataRequest request)
    {
        var klineRequest = Mapping.ToKlineDataRequest(request);
        var klineDataResult = await _externalClient.GetKlineData(klineRequest);
        return klineDataResult.IsFailed
            ? Result.Fail(klineDataResult.Errors)
            : Result.Ok(klineDataResult.Value.Klines);
    }

    private static class Mapping
    {
        /// <summary>
        /// Converts an API request to a default service client request.
        /// </summary>
        public static SvcExternal.Requests.KlineDataRequest ToDefaultKlineDataRequest(
            Coin coin,
            TradingPair tradingPair
        ) =>
            new()
            {
                CoinMain = ToKlineDataRequestCoin(coin),
                TradingPair = ToKlineDataRequestTradingPair(tradingPair),
                Interval = DefaultInterval,
                StartTime = DefaultStartTime,
                EndTime = DefaultEndTime,
                Limit = DefaultLimit,
            };

        private static SvcExternal.Requests.KlineDataRequestCoinBase ToKlineDataRequestCoin(
            Coin coin
        ) =>
            new()
            {
                Id = coin.Id,
                Symbol = coin.Symbol,
                Name = coin.Name,
            };

        private static SvcExternal.Requests.KlineDataRequestTradingPair ToKlineDataRequestTradingPair(
            TradingPair tradingPair
        ) =>
            new()
            {
                Id = tradingPair.Id,
                CoinQuote = ToKlineDataRequestCoinQuote(tradingPair.CoinQuote),
                Exchanges = tradingPair.Exchanges,
            };

        private static SvcExternal.Requests.KlineDataRequestCoinQuote ToKlineDataRequestCoinQuote(
            TradingPairCoinQuote coinQuote
        ) =>
            new()
            {
                Id = coinQuote.Id,
                Symbol = coinQuote.Symbol,
                Name = coinQuote.Name,
            };

        /// <summary>
        /// Converts an API request to a service client request.
        /// </summary>
        public static SvcExternal.Requests.KlineDataRequest ToKlineDataRequest(
            KlineDataRequest request
        ) =>
            new()
            {
                CoinMain = ToKlineDataRequestCoin(request.CoinMain),
                TradingPair = ToKlineDataRequestTradingPair(request),
                Interval = request.Interval,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                Limit = request.Limit,
            };

        private static SvcExternal.Requests.KlineDataRequestCoinBase ToKlineDataRequestCoin(
            KlineDataRequestCoin request
        ) =>
            new()
            {
                Id = request.Id,
                Symbol = request.Symbol,
                Name = request.Name,
            };

        private static SvcExternal.Requests.KlineDataRequestTradingPair ToKlineDataRequestTradingPair(
            KlineDataRequest request
        ) =>
            new()
            {
                Id = request.IdTradingPair,
                CoinQuote = ToKlineDataRequestCoinQuote(request.CoinQuote),
                Exchanges = request.Exchanges,
            };

        private static SvcExternal.Requests.KlineDataRequestCoinQuote ToKlineDataRequestCoinQuote(
            KlineDataRequestCoin request
        ) =>
            new()
            {
                Id = request.Id,
                Symbol = request.Symbol,
                Name = request.Name,
            };
    }
}
