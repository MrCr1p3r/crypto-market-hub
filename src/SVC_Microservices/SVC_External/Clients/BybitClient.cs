using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using SharedLibrary.Enums;
using SharedLibrary.Extensions;
using SVC_External.Clients.Interfaces;
using SVC_External.Models.Input;
using SVC_External.Models.Output;

namespace SVC_External.Clients;

/// <summary>
/// Implements methods for interracting with Bybit API.
/// </summary>
public class BybitClient(IHttpClientFactory httpClientFactory, ILogger<BybitClient> logger)
    : IExchangeClient
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("BybitClient");
    private readonly ILogger<BybitClient> _logger = logger;

    /// <inheritdoc />
    public async Task<IEnumerable<KlineData>> GetKlineData(KlineDataRequestFormatted request)
    {
        var endpoint = Mapping.ToBybitKlineEndpoint(request);
        var httpResponse = await _httpClient.GetAsync(endpoint);
        if (!httpResponse.IsSuccessStatusCode)
        {
            await _logger.LogUnsuccessfulHttpResponse(httpResponse);
            return [];
        }

        var responseBody = await httpResponse.Content.ReadAsStringAsync();
        var klineResponse = JsonSerializer.Deserialize<ResponseDtos.BybitKlineResponse>(
            responseBody
        );
        return klineResponse!.Result.List!.Select(data => Mapping.ToKlineData(request, data));
    }

    /// <inheritdoc />
    public async Task<ListedCoins> GetAllListedCoins(ListedCoins listedCoins)
    {
        var endpoint = "/v5/market/instruments-info?category=linear";
        var httpResponse = await _httpClient.GetAsync(endpoint);
        if (!httpResponse.IsSuccessStatusCode)
        {
            await _logger.LogUnsuccessfulHttpResponse(httpResponse);
            return listedCoins;
        }

        var responseBody = await httpResponse.Content.ReadAsStringAsync();
        var bybitResponse = JsonSerializer.Deserialize<ResponseDtos.BybitReponse>(responseBody);
        listedCoins.BybitCoins = bybitResponse!.Result.SymbolsList.Select(symbol =>
            symbol.BaseCoin
        );
        return listedCoins;
    }

    private static class ResponseDtos
    {
        public class BybitKlineResponse
        {
            [JsonPropertyName("result")]
            public BybitKlineResult Result { get; set; } = new();
        }

        public class BybitKlineResult
        {
            [JsonPropertyName("list")]
            public List<List<string>> List { get; set; } = [];
        }

        public class BybitReponse
        {
            [JsonPropertyName("result")]
            public BybitResult Result { get; set; } = new();
        }

        public class BybitResult
        {
            [JsonPropertyName("list")]
            public HashSet<BybitSymbol> SymbolsList { get; set; } = [];
        }

        public record BybitSymbol
        {
            [JsonPropertyName("baseCoin")]
            public string BaseCoin { get; set; } = string.Empty;
        }
    }

    private static class Mapping
    {
        public static string ToBybitKlineEndpoint(KlineDataRequestFormatted request) =>
            $"/v5/market/kline?category=linear"
            + $"&symbol={request.CoinMain}{request.CoinQuote}"
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

        public static KlineData ToKlineData(KlineDataRequestFormatted request, List<string> data)
        {
            var ic = CultureInfo.InvariantCulture;
            return new()
            {
                OpenTime = Convert.ToInt64(data[0]),
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
            var openTime = long.Parse(openTimeString);
            var durationInMinutes = (long)interval;
            return openTime + durationInMinutes * 60 * 1000;
        }
    }
}
