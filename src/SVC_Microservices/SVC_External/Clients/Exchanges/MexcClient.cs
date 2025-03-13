using System.Collections.Frozen;
using System.Globalization;
using System.Text.Json;
using FluentResults;
using SharedLibrary.Enums;
using SharedLibrary.Extensions;
using SVC_External.Clients.Exchanges.Interfaces;
using SVC_External.Models.Exchanges.ClientResponses;
using SVC_External.Models.Exchanges.Input;
using SVC_External.Models.Exchanges.Output;

namespace SVC_External.Clients.Exchanges;

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
    public async Task<Result<IEnumerable<ExchangeKlineData>>> GetKlineData(
        ExchangeKlineDataRequest request
    )
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
                ExchangeKlineInterval.OneHour => "1h",
                ExchangeKlineInterval.FourHours => "4h",
                ExchangeKlineInterval.OneDay => "1d",
                ExchangeKlineInterval.OneWeek => "1w",
                ExchangeKlineInterval.OneMonth => "1M",
                _ => throw new ArgumentException($"Unsupported TimeFrame: {timeFrame}"),
            };

        public static ExchangeKlineData ToKlineData(List<JsonElement> data)
        {
            var ic = CultureInfo.InvariantCulture;
            return new()
            {
                OpenTime = data[0].GetInt64(),
                OpenPrice = Convert.ToDecimal(data[1].GetString(), ic),
                HighPrice = Convert.ToDecimal(data[2].GetString(), ic),
                LowPrice = Convert.ToDecimal(data[3].GetString(), ic),
                ClosePrice = Convert.ToDecimal(data[4].GetString(), ic),
                Volume = Convert.ToDecimal(data[5].GetString(), ic),
                CloseTime = data[6].GetInt64(),
            };
        }
        #endregion
    }
}
