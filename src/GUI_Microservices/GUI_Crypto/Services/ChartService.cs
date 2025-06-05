using FluentResults;
using GUI_Crypto.ApiContracts.Requests.KlineData;
using GUI_Crypto.ApiContracts.Responses;
using GUI_Crypto.MicroserviceClients.SvcCoins;
using GUI_Crypto.MicroserviceClients.SvcExternal;
using GUI_Crypto.ServiceModels;
using GUI_Crypto.Services.Interfaces;
using SharedLibrary.Enums;
using SvcExternal = GUI_Crypto.MicroserviceClients.SvcExternal.Contracts;

namespace GUI_Crypto.Services;

/// <summary>
/// Service for handling chart-related operations.
/// </summary>
public class ChartService(ISvcCoinsClient coinsClient, ISvcExternalClient externalClient)
    : IChartService
{
    public const ExchangeKlineInterval DefaultInterval = ExchangeKlineInterval.FifteenMinutes;
    public const int DefaultLimit = 1000;
    public static readonly DateTime DefaultStartTime = DateTime.UtcNow.AddDays(-7);
    public static readonly DateTime DefaultEndTime = DateTime.UtcNow;
    private readonly ISvcCoinsClient _coinsClient = coinsClient;
    private readonly ISvcExternalClient _externalClient = externalClient;

    /// <inheritdoc/>
    public async Task<Result<ChartData>> GetChartData(KlineDataRequest request)
    {
        var klineRequest = Mapping.ToDefaultKlineDataRequest(request);

        var klineTask = _externalClient.GetKlineData(klineRequest);
        var coinTask = _coinsClient.GetCoinById(request.CoinMain.Id);
        await Task.WhenAll(klineTask, coinTask);

        var klineResult = await klineTask;
        if (klineResult.IsFailed)
        {
            return Result.Fail(klineResult.Errors);
        }

        var coinResult = await coinTask;
        if (coinResult.IsFailed)
        {
            return Result.Fail(coinResult.Errors);
        }

        var chartData = new ChartData
        {
            Coin = coinResult.Value,
            KlineResponse = klineResult.Value,
        };
        return Result.Ok(chartData);
    }

    /// <inheritdoc/>
    public async Task<Result<IEnumerable<Kline>>> GetKlineData(KlineDataRequest request)
    {
        var klineRequest = Mapping.ToKlineDataRequest(request);
        var klineDataResult = await _externalClient.GetKlineData(klineRequest);
        if (klineDataResult.IsFailed)
        {
            return Result.Fail(klineDataResult.Errors);
        }

        var response = klineDataResult.Value.KlineData.Select(Mapping.ToKlineDataApiResponse);
        return Result.Ok(response);
    }

    private static class Mapping
    {
        /// <summary>
        /// Converts an API request to a default service client request.
        /// </summary>
        public static SvcExternal.Requests.KlineDataRequest ToDefaultKlineDataRequest(
            KlineDataRequest request
        ) =>
            new()
            {
                CoinMain = ToKlineDataRequestCoin(request.CoinMain),
                TradingPair = ToKlineDataRequestTradingPair(request),
                Interval = DefaultInterval,
                StartTime = DefaultStartTime,
                EndTime = DefaultEndTime,
                Limit = DefaultLimit,
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

        /// <summary>
        /// Converts a service client response to an API response.
        /// </summary>
        public static Kline ToKlineDataApiResponse(
            SvcExternal.Responses.KlineData.KlineData klineData
        ) =>
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
    }
}
