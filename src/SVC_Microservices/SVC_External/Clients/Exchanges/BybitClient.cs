using System.Globalization;
using FluentResults;
using SharedLibrary.Enums;
using SharedLibrary.Extensions;
using SVC_External.Clients.Exchanges.Interfaces;
using SVC_External.Models.Exchanges.ClientResponses;
using SVC_External.Models.Exchanges.Input;
using SVC_External.Models.Exchanges.Output;

namespace SVC_External.Clients.Exchanges;

/// <summary>
/// Implements methods for interracting with Bybit API.
/// </summary>
public class BybitClient(IHttpClientFactory httpClientFactory, ILogger<BybitClient> logger)
    : IExchangesClient
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("BybitClient");
    private readonly ILogger<BybitClient> _logger = logger;

    /// <inheritdoc />
    public Exchange CurrentExchange => Exchange.Bybit;

    /// <inheritdoc />
    public async Task<Result<IEnumerable<ExchangeCoin>>> GetAllSpotCoins()
    {
        var endpoint = "/v5/market/instruments-info?category=spot";
        var response = await _httpClient.GetFromJsonSafeAsync<BybitDtos.BybitSpotAssetsResponse>(
            endpoint,
            _logger,
            "Failed to retrieve spot coins from Bybit"
        );

        return response.IsSuccess
            ? Result.Ok(Mapping.ToOutputCoins(response.Value.Result.TradingPairs))
            : Result.Fail(response.Errors);
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<ExchangeKlineData>>> GetKlineData(
        ExchangeKlineDataRequest request
    )
    {
        var endpoint = Mapping.ToBybitKlineEndpoint(request);
        var response = await _httpClient.GetFromJsonSafeAsync<BybitDtos.BybitKlineResponse>(
            endpoint,
            _logger,
            "Failed to retrieve kline data from Bybit"
        );

        return response.IsSuccess
            ? Result.Ok(
                response.Value.Result.List.Select(data => Mapping.ToKlineData(request, data))
            )
            : Result.Fail(response.Errors);
    }

    private static class Mapping
    {
        #region GetAllSpotCoins
        public static IEnumerable<ExchangeCoin> ToOutputCoins(
            IEnumerable<BybitDtos.TradingPair> bybitSymbols
        ) =>
            bybitSymbols
                .GroupBy(symbol => symbol.BaseAssetSymbol)
                .Select(group => ToCoin(group.Key, group));

        public static ExchangeCoin ToCoin(
            string baseCoin,
            IEnumerable<BybitDtos.TradingPair> bybitSymbols
        ) => new() { Symbol = baseCoin, TradingPairs = bybitSymbols.Select(ToTradingPair) };

        private static ExchangeTradingPair ToTradingPair(BybitDtos.TradingPair bybitSymbol) =>
            new()
            {
                CoinQuote = new() { Symbol = bybitSymbol.QuoteAssetSymbol },
                ExchangeInfo = ToExchangeInfo(bybitSymbol.TradingStatus),
            };

        private static ExchangeTradingPairExchangeInfo ToExchangeInfo(
            BybitDtos.TradingPairStatus status
        ) =>
            new()
            {
                Exchange = Exchange.Bybit,
                Status = status switch
                {
                    BybitDtos.TradingPairStatus.Trading => ExchangeTradingPairStatus.Available,
                    BybitDtos.TradingPairStatus.PreLaunch =>
                        ExchangeTradingPairStatus.CurrentlyUnavailable,
                    BybitDtos.TradingPairStatus.Settling =>
                        ExchangeTradingPairStatus.CurrentlyUnavailable,
                    BybitDtos.TradingPairStatus.Delivering =>
                        ExchangeTradingPairStatus.CurrentlyUnavailable,
                    BybitDtos.TradingPairStatus.Closed => ExchangeTradingPairStatus.Unavailable,
                    _ => throw new ArgumentException($"Unsupported Status: {status}"),
                },
            };
        #endregion

        #region GetKlineData
        public static string ToBybitKlineEndpoint(ExchangeKlineDataRequest request) =>
            $"/v5/market/kline?category=spot"
            + $"&symbol={request.CoinMainSymbol}{request.CoinQuoteSymbol}"
            + $"&interval={ToBybitTimeFrame(request.Interval)}"
            + $"&start={request.StartTimeUnix}"
            + $"&end={request.EndTimeUnix}"
            + $"&limit={request.Limit}";

        public static string ToBybitTimeFrame(ExchangeKlineInterval interval) =>
            interval switch
            {
                ExchangeKlineInterval.OneMinute => "1",
                ExchangeKlineInterval.FiveMinutes => "5",
                ExchangeKlineInterval.FifteenMinutes => "15",
                ExchangeKlineInterval.ThirtyMinutes => "30",
                ExchangeKlineInterval.OneHour => "60",
                ExchangeKlineInterval.FourHours => "240",
                ExchangeKlineInterval.OneDay => "D",
                ExchangeKlineInterval.OneWeek => "W",
                ExchangeKlineInterval.OneMonth => "M",
                _ => throw new ArgumentException($"Unsupported TimeFrame: {interval}"),
            };

        public static ExchangeKlineData ToKlineData(
            ExchangeKlineDataRequest request,
            List<string> data
        )
        {
            var ic = CultureInfo.InvariantCulture;
            return new()
            {
                OpenTime = Convert.ToInt64(data[0], ic),
                OpenPrice = Convert.ToDecimal(data[1], ic),
                HighPrice = Convert.ToDecimal(data[2], ic),
                LowPrice = Convert.ToDecimal(data[3], ic),
                ClosePrice = Convert.ToDecimal(data[4], ic),
                Volume = Convert.ToDecimal(data[5], ic),
                CloseTime = CalculateCloseTime(data[0], request.Interval),
            };
        }

        public static long CalculateCloseTime(string openTimeString, ExchangeKlineInterval interval)
        {
            var openTime = long.Parse(openTimeString, CultureInfo.InvariantCulture);
            var durationInMinutes = (long)interval;
            return openTime + (durationInMinutes * 60 * 1000);
        }
        #endregion
    }
}
