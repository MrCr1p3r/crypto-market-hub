using System.Globalization;
using System.Text.Json;
using SharedLibrary.Enums;
using SVC_External.Clients.Interfaces;
using SVC_External.Models.Input;
using SVC_External.Models.Output;

namespace SVC_External.Clients;

/// <summary>
/// Implements methods for interracting with Bybit API.
/// </summary>
public class BybitClient(IHttpClientFactory httpClientFactory) : IExchangeClient
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("BybitClient");

    /// <inheritdoc />
    public async Task<IEnumerable<KlineData>> GetKlineData(KlineDataRequestFormatted request)
    {
        var interval = Mapping.ToBybitTimeFrame(request.Interval);

        var endpoint = $"/v5/market/kline?category=linear";
        endpoint += $"&symbol={request.CoinMain}{request.CoinQuote}";
        endpoint += $"&interval={interval}";
        endpoint += $"&start={request.StartTimeUnix}";
        endpoint += $"&end={request.EndTimeUnix}";
        endpoint += $"&limit={request.Limit}";

        var response = await _httpClient.GetAsync(endpoint);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        var jsonDocument = JsonDocument.Parse(responseBody);

        var rawData = jsonDocument.RootElement
            .GetProperty("result")
            .GetProperty("list")
            .EnumerateArray();

        return rawData.Select(data => new KlineData
        {
            OpenTime = long.Parse(data[0].GetString()!),
            OpenPrice = decimal.Parse(data[1].GetString()!, CultureInfo.InvariantCulture),
            HighPrice = decimal.Parse(data[2].GetString()!, CultureInfo.InvariantCulture),
            LowPrice = decimal.Parse(data[3].GetString()!, CultureInfo.InvariantCulture),
            ClosePrice = decimal.Parse(data[4].GetString()!, CultureInfo.InvariantCulture),
            Volume = decimal.Parse(data[5].GetString()!, CultureInfo.InvariantCulture),
            CloseTime = Mapping.CalculateCloseTime(data[0].GetString()!, request.Interval)
        });
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetAllListedCoins()
    {
        var endpoint = "/v5/market/instruments-info?category=linear";
        var response = await _httpClient.GetAsync(endpoint);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();

        using JsonDocument document = JsonDocument.Parse(responseBody);

        if (!document.RootElement.TryGetProperty("result", out JsonElement result))
        {
            return [];
        }

        if (!result.TryGetProperty("list", out JsonElement list))
        {
            return [];
        }

        var baseCoins = new HashSet<string>();

        foreach (var instrument in list.EnumerateArray())
        {
            if (!instrument.TryGetProperty("baseCoin", out JsonElement baseCoinElement))
            {
                continue;
            }
            var baseCoin = baseCoinElement.GetString();
            if (!string.IsNullOrEmpty(baseCoin))
            {
                baseCoins.Add(baseCoin);
            }
        }

        return baseCoins;
    }

    private static class Mapping
    {
        public static string ToBybitTimeFrame(ExchangeKlineInterval interval) => interval switch
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
            _ => throw new ArgumentException($"Unsupported TimeFrame: {interval}")
        };

        public static long CalculateCloseTime(string openTimeString, ExchangeKlineInterval interval)
        {
            var openTime = long.Parse(openTimeString);
            var durationInMinutes = (long)interval;
            return openTime + durationInMinutes * 60 * 1000;
        }
    }
}
