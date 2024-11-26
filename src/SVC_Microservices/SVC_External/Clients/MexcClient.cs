using System.Globalization;
using System.Text.Json;
using SharedLibrary.Enums;
using SVC_External.Clients.Interfaces;
using SVC_External.Models.Input;
using SVC_External.Models.Output;

namespace SVC_External.Clients;

/// <summary>
/// Implements methods for interracting with Mexc API.
/// </summary>
public class MexcClient(IHttpClientFactory httpClientFactory) : IExchangeClient
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("MexcClient");

    /// <inheritdoc />
    public async Task<IEnumerable<KlineData>> GetKlineData(KlineDataRequestFormatted request)
    {
        var interval = Mapping.ToMexcTimeFrame(request.Interval);
        var endpoint = $"/api/v3/klines?symbol={request.CoinMain + request.CoinQuote}";
        endpoint += $"&interval={interval}";
        endpoint += $"&limit={request.Limit}";
        endpoint += $"&startTime={request.StartTimeUnix}";
        endpoint += $"&endTime={request.EndTimeUnix}";

        var response = await _httpClient.GetAsync(endpoint);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        var rawData = JsonSerializer.Deserialize<List<List<JsonElement>>>(responseBody);

        return rawData?.Select(data => new KlineData
        {
            OpenTime = data[0].GetInt64(),
            OpenPrice = decimal.Parse(data[1].GetString()!, CultureInfo.InvariantCulture),
            HighPrice = decimal.Parse(data[2].GetString()!, CultureInfo.InvariantCulture),
            LowPrice = decimal.Parse(data[3].GetString()!, CultureInfo.InvariantCulture),
            ClosePrice = decimal.Parse(data[4].GetString()!, CultureInfo.InvariantCulture),
            Volume = decimal.Parse(data[5].GetString()!),
            CloseTime = data[6].GetInt64()
        }) ?? [];
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetAllListedCoins()
    {
        var endpoint = "/api/v3/exchangeInfo";
        var response = await _httpClient.GetAsync(endpoint);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();

        using JsonDocument document = JsonDocument.Parse(responseBody);
        if (!document.RootElement.TryGetProperty("symbols", out JsonElement symbols))
        {
            return [];
        }

        var baseAssets = new HashSet<string>();

        foreach (var symbol in symbols.EnumerateArray())
        {
            if (!symbol.TryGetProperty("baseAsset", out JsonElement baseAssetElement))
            {
                continue;
            }
            var baseAsset = baseAssetElement.GetString();
            if (!string.IsNullOrEmpty(baseAsset))
            {
                baseAssets.Add(baseAsset);
            }
        }

        return baseAssets;
    }

    private static class Mapping
    {
        public static string ToMexcTimeFrame(ExchangeKlineInterval timeFrame) => timeFrame switch
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
            _ => throw new ArgumentException($"Unsupported TimeFrame: {timeFrame}")
        };
    }
}
