using System.Text.Json;
using FluentResults;
using SharedLibrary.Enums;
using SharedLibrary.Extensions.HttpClient.External;
using SharedLibrary.Models;
using SVC_External.ExternalClients.Exchanges.Contracts.Requests;
using SVC_External.ExternalClients.Exchanges.Contracts.Responses;

namespace SVC_External.ExternalClients.Exchanges.Binance;

/// <summary>
/// Implements methods for interracting with Binance API.
/// </summary>
public class BinanceClient(IHttpClientFactory httpClientFactory, ILogger<BinanceClient> logger)
    : IExchangesClient
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("BinanceClient");
    private readonly ILogger<BinanceClient> _logger = logger;

    /// <inheritdoc />
    public Exchange CurrentExchange => Exchange.Binance;

    /// <inheritdoc />
    public async Task<Result<IEnumerable<ExchangeCoin>>> GetAllSpotCoins()
    {
        var endpoint = "/api/v3/exchangeInfo";
        // Disabled to reduce fetch time and response size.
        endpoint += "?showPermissionSets=false";
        var response = await _httpClient.GetFromJsonSafeAsync<BinanceDtos.Response>(
            endpoint,
            _logger,
            "Failed to retrieve spot coins from Binance."
        );

        return response.IsSuccess
            ? Result.Ok(Mapping.ToOutputCoins(response.Value.TradingPairs))
            : Result.Fail(response.Errors);
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<Kline>>> GetKlineData(ExchangeKlineDataRequest request)
    {
        var endpoint = Mapping.ToBinanceKlineEndpoint(request);
        var response = await _httpClient.GetFromJsonSafeAsync<List<List<JsonElement>>>(
            endpoint,
            _logger,
            "Failed to retrieve kline data from Binance."
        );

        return response.IsSuccess
            ? Result.Ok(response.Value.Select(Mapping.ToKlineData))
            : Result.Fail(response.Errors);
    }

    private static class Mapping
    {
        #region GetAllSpotCoins
        public static IEnumerable<ExchangeCoin> ToOutputCoins(
            IEnumerable<BinanceDtos.TradingPair> tradingPairs
        ) =>
            tradingPairs
                .GroupBy(tradingPair => tradingPair.BaseAssetSymbol)
                .Select(group => ToCoin(group.Key, group));

        public static ExchangeCoin ToCoin(
            string baseAsset,
            IEnumerable<BinanceDtos.TradingPair> tradingPairs
        ) => new() { Symbol = baseAsset, TradingPairs = tradingPairs.Select(ToOutputTradingPair) };

        private static ExchangeTradingPair ToOutputTradingPair(
            BinanceDtos.TradingPair tradingPair
        ) =>
            new()
            {
                CoinQuote = new() { Symbol = tradingPair.QuoteAssetSymbol },
                ExchangeInfo = ToExchangeInfo(tradingPair.Status),
            };

        private static ExchangeTradingPairExchangeInfo ToExchangeInfo(
            BinanceDtos.TradingPairStatus status
        ) =>
            new()
            {
                Exchange = Exchange.Binance,
                Status = status switch
                {
                    BinanceDtos.TradingPairStatus.TRADING => ExchangeTradingPairStatus.Available,
                    BinanceDtos.TradingPairStatus.HALT =>
                        ExchangeTradingPairStatus.CurrentlyUnavailable,
                    BinanceDtos.TradingPairStatus.BREAK => ExchangeTradingPairStatus.Unavailable,
                    _ => throw new ArgumentException($"Unsupported Status: {status}"),
                },
            };
        #endregion

        #region GetKlineData
        public static string ToBinanceKlineEndpoint(ExchangeKlineDataRequest request) =>
            $"/api/v3/klines?symbol={request.CoinMainSymbol + request.CoinQuoteSymbol}"
            + $"&interval={ToBinanceTimeFrame(request.Interval)}"
            + $"&limit={request.Limit}"
            + $"&startTime={request.StartTimeUnix}"
            + $"&endTime={request.EndTimeUnix}";

        public static string ToBinanceTimeFrame(ExchangeKlineInterval timeFrame) =>
            timeFrame switch
            {
                ExchangeKlineInterval.OneMinute => "1m",
                ExchangeKlineInterval.FiveMinutes => "5m",
                ExchangeKlineInterval.FifteenMinutes => "15m",
                ExchangeKlineInterval.ThirtyMinutes => "30m",
                ExchangeKlineInterval.OneHour => "1h",
                ExchangeKlineInterval.FourHours => "4h",
                ExchangeKlineInterval.OneDay => "1d",
                ExchangeKlineInterval.OneWeek => "1w",
                ExchangeKlineInterval.OneMonth => "1M",
                _ => throw new ArgumentException($"Unsupported TimeFrame: {timeFrame}"),
            };

        public static Kline ToKlineData(List<JsonElement> data) =>
            new()
            {
                OpenTime = data[0].GetInt64(),
                OpenPrice = data[1].GetString() ?? string.Empty,
                HighPrice = data[2].GetString() ?? string.Empty,
                LowPrice = data[3].GetString() ?? string.Empty,
                ClosePrice = data[4].GetString() ?? string.Empty,
                Volume = data[5].GetString() ?? string.Empty,
                CloseTime = data[6].GetInt64(),
            };
        #endregion
    }
}
