using System.Collections.Frozen;
using System.Text.Json;
using FluentResults;
using SharedLibrary.Enums;
using SharedLibrary.Extensions.HttpClient.External;
using SharedLibrary.Models;
using SVC_External.ExternalClients.Exchanges.Contracts.Requests;
using SVC_External.ExternalClients.Exchanges.Contracts.Responses;

namespace SVC_External.ExternalClients.Exchanges.Mexc;

/// <summary>
/// Implements methods for interracting with Mexc API.
/// </summary>
public class MexcClient(IHttpClientFactory httpClientFactory, ILogger<MexcClient> logger)
    : IExchangesClient
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("MexcClient");
    private readonly ILogger<MexcClient> _logger = logger;

    /// <inheritdoc />
    public Exchange CurrentExchange => Exchange.Mexc;

    /// <inheritdoc />
    public async Task<Result<IEnumerable<ExchangeCoin>>> GetAllSpotCoins()
    {
        var endpoint = "/api/v3/exchangeInfo";
        var response = await _httpClient.GetFromJsonSafeAsync<MexcDtos.Response>(
            endpoint,
            _logger,
            "Failed to retrieve spot coins from Mexc"
        );

        return response.IsSuccess
            ? Result.Ok(Mapping.ToOutputCoins(response.Value.TradingPairs))
            : Result.Fail(response.Errors);
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<Kline>>> GetKlineData(ExchangeKlineDataRequest request)
    {
        var endpoint = Mapping.ToMexcKlineEndpoint(request);
        var response = await _httpClient.GetFromJsonSafeAsync<List<List<JsonElement>>>(
            endpoint,
            _logger,
            "Failed to retrieve kline data from Mexc"
        );

        return response.IsSuccess
            ? Result.Ok(response.Value.Select(Mapping.ToKlineData))
            : Result.Fail(response.Errors);
    }

    private static class Mapping
    {
        #region GetAllSpotCoins
        public static IEnumerable<ExchangeCoin> ToOutputCoins(
            IEnumerable<MexcDtos.TradingPair> mexcSymbols
        )
        {
            var symbolToName = mexcSymbols
                .DistinctBy(s => s.BaseAssetSymbol, StringComparer.OrdinalIgnoreCase)
                .ToFrozenDictionary(
                    s => s.BaseAssetSymbol,
                    s => s.BaseAssetFullName,
                    StringComparer.OrdinalIgnoreCase
                );

            return mexcSymbols
                .GroupBy(symbol => symbol.BaseAssetSymbol)
                .Select(group => ToCoin(group.Key, group, symbolToName));
        }

        public static ExchangeCoin ToCoin(
            string baseAsset,
            IEnumerable<MexcDtos.TradingPair> mexcSymbols,
            FrozenDictionary<string, string> symbolToName
        ) =>
            new()
            {
                Symbol = baseAsset,
                Name = mexcSymbols.First().BaseAssetFullName,
                TradingPairs = mexcSymbols.Select(s => ToTradingPair(s, symbolToName)),
            };

        private static ExchangeTradingPair ToTradingPair(
            MexcDtos.TradingPair symbol,
            FrozenDictionary<string, string> symbolToName
        ) =>
            new()
            {
                CoinQuote = ToCoinQuote(symbol.QuoteAssetSymbol, symbolToName),
                ExchangeInfo = ToExchangeInfo(symbol.Status),
            };

        private static ExchangeTradingPairCoinQuote ToCoinQuote(
            string symbolQuote,
            FrozenDictionary<string, string> symbolToName
        ) => new() { Symbol = symbolQuote, Name = symbolToName.GetValueOrDefault(symbolQuote) };

        private static ExchangeTradingPairExchangeInfo ToExchangeInfo(
            MexcDtos.TradingPairStatus status
        ) =>
            new()
            {
                Exchange = Exchange.Mexc,
                Status = status switch
                {
                    MexcDtos.TradingPairStatus.Trading => ExchangeTradingPairStatus.Available,
                    MexcDtos.TradingPairStatus.CurrentlyUnavailable =>
                        ExchangeTradingPairStatus.CurrentlyUnavailable,
                    MexcDtos.TradingPairStatus.Unavailable => ExchangeTradingPairStatus.Unavailable,
                    _ => throw new ArgumentException($"Unsupported Status: {status}"),
                },
            };
        #endregion

        #region GetKlineData
        public static string ToMexcKlineEndpoint(ExchangeKlineDataRequest request) =>
            $"/api/v3/klines?symbol={request.CoinMainSymbol + request.CoinQuoteSymbol}"
            + $"&interval={ToMexcTimeFrame(request.Interval)}"
            + $"&limit={request.Limit}"
            + $"&startTime={request.StartTimeUnix}"
            + $"&endTime={request.EndTimeUnix}";

        public static string ToMexcTimeFrame(ExchangeKlineInterval timeFrame) =>
            timeFrame switch
            {
                ExchangeKlineInterval.OneMinute => "1m",
                ExchangeKlineInterval.FiveMinutes => "5m",
                ExchangeKlineInterval.FifteenMinutes => "15m",
                ExchangeKlineInterval.ThirtyMinutes => "30m",
                ExchangeKlineInterval.OneHour => "60m",
                ExchangeKlineInterval.FourHours => "4h",
                ExchangeKlineInterval.OneDay => "1d",
                ExchangeKlineInterval.OneWeek => "1W",
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
