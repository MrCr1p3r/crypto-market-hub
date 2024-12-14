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
/// Implements methods for interracting with Mexc API.
/// </summary>
public class MexcClient(IHttpClientFactory httpClientFactory, ILogger<MexcClient> logger)
    : IExchangeClient
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("MexcClient");
    private readonly ILogger<MexcClient> _logger = logger;

    /// <inheritdoc />
    public async Task<IEnumerable<KlineData>> GetKlineData(KlineDataRequestFormatted request)
    {
        var endpoint = Mapping.ToMexcKlineEndpoint(request);
        var httpResponse = await _httpClient.GetAsync(endpoint);
        if (!httpResponse.IsSuccessStatusCode)
        {
            await _logger.LogUnsuccessfulHttpResponse(httpResponse);
            return [];
        }

        var responseBody = await httpResponse.Content.ReadAsStringAsync();
        var rawData = JsonSerializer.Deserialize<List<List<JsonElement>>>(responseBody);
        return rawData!.Select(Mapping.ToKlineData);
    }

    /// <inheritdoc />
    public async Task<ListedCoins> GetAllListedCoins(ListedCoins listedCoins)
    {
        var endpoint = "/api/v3/exchangeInfo";
        var httpResponse = await _httpClient.GetAsync(endpoint);
        if (!httpResponse.IsSuccessStatusCode)
        {
            await _logger.LogUnsuccessfulHttpResponse(httpResponse);
            return listedCoins;
        }

        var responseBody = await httpResponse.Content.ReadAsStringAsync();
        var mexcResponse = JsonSerializer.Deserialize<ResponseDtos.MexcResponse>(responseBody);
        listedCoins.MexcCoins = mexcResponse!.Symbols.Select(symbol => symbol.BaseAsset);
        return listedCoins;
    }

    private static class ResponseDtos
    {
        public class MexcResponse
        {
            [JsonPropertyName("symbols")]
            public HashSet<MexcSymbol> Symbols { get; set; } = [];
        }

        public record MexcSymbol
        {
            [JsonPropertyName("baseAsset")]
            public string BaseAsset { get; set; } = string.Empty;
        }
    }

    private static class Mapping
    {
        public static string ToMexcKlineEndpoint(KlineDataRequestFormatted request) =>
            $"/api/v3/klines?symbol={request.CoinMain + request.CoinQuote}"
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

        public static KlineData ToKlineData(List<JsonElement> data)
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
    }
}
