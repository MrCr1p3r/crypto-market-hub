using System.Globalization;
using System.Text.Json;
using SharedLibrary.Enums;
using SharedLibrary.Extensions;
using SVC_External.Clients.Exchanges.Interfaces;
using SVC_External.Models.Exchanges.ClientResponses;
using SVC_External.Models.Exchanges.Input;
using SVC_External.Models.Exchanges.Output;

namespace SVC_External.Clients.Exchanges;

/// <summary>
/// Implements methods for interracting with Binance API.
/// </summary>
public class BinanceClient(IHttpClientFactory httpClientFactory, ILogger<BinanceClient> logger)
    : IExchangesClient
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("BinanceClient");
    private readonly ILogger<BinanceClient> _logger = logger;

    /// <inheritdoc />
    public async Task<IEnumerable<ExchangeCoin>> GetAllSpotCoins()
    {
        var endpoint = "/api/v3/exchangeInfo";
        endpoint += "?showPermissionSets=false"; // Disabled to reduce fetch time and response size.
        var httpResponse = await _httpClient.GetAsync(endpoint);

        if (!httpResponse.IsSuccessStatusCode)
        {
            await _logger.LogUnsuccessfulHttpResponse(httpResponse);
            return [];
        }

        var binanceResponse = await httpResponse.Content.ReadFromJsonAsync<BinanceDtos.Response>();
        return Mapping.ToOutputCoins(binanceResponse!.TradingPairs);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ExchangeKlineData>> GetKlineData(ExchangeKlineDataRequest request)
    {
        var endpoint = Mapping.ToBinanceKlineEndpoint(request);
        var httpResponse = await _httpClient.GetAsync(endpoint);
        if (!httpResponse.IsSuccessStatusCode)
        {
            await _logger.LogUnsuccessfulHttpResponse(httpResponse);
            return [];
        }

        var rawData = await httpResponse.Content.ReadFromJsonAsync<List<List<JsonElement>>>();
        return rawData!.Select(Mapping.ToKlineData);
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
            $"/api/v3/klines?symbol={request.CoinMain + request.CoinQuote}"
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
