using System.Collections.Frozen;
using SharedLibrary.Extensions;
using SVC_External.Clients.MarketDataProviders.Interfaces;
using SVC_External.Models.MarketDataProviders.Output;
using static SVC_External.Models.MarketDataProviders.ClientResponses.CoinGeckoDtos;

namespace SVC_External.Clients.MarketDataProviders;

/// <summary>
/// Implements methods for interacting with CoinGecko API.
/// </summary>
public partial class CoinGeckoClient(
    IHttpClientFactory httpClientFactory,
    ILogger<CoinGeckoClient> logger
) : ICoinGeckoClient
{
    private const int MaxIdsPerRequest = 250;
    private const int MaxTickersPerRequest = 100;
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("CoinGeckoClient");
    private readonly ILogger<CoinGeckoClient> _logger = logger;

    /// <inheritdoc />
    public async Task<IEnumerable<CoinCoinGecko>> GetCoinsList()
    {
        var endpoint = "/api/v3/coins/list";
        var httpResponse = await _httpClient.GetAsync(endpoint);
        if (!httpResponse.IsSuccessStatusCode)
        {
            await _logger.LogUnsuccessfulHttpResponse(httpResponse);
            return [];
        }

        var response = await httpResponse.Content.ReadFromJsonAsync<
            IEnumerable<CoinListResponse>
        >();
        return response!.Select(Mapping.ToCoinCoinGecko);
    }

    /// <inheritdoc />
    public async Task<FrozenDictionary<string, string?>> GetSymbolToIdMapForExchange(
        string idExchange
    )
    {
        var allTickers = new List<TickerData>();
        for (var page = 1; ; page++)
        {
            var endpoint =
                $"/api/v3/exchanges/{idExchange}/tickers?depth=false&order=volume_desc&page={page}";
            var httpResponse = await _httpClient.GetAsync(endpoint);
            if (!httpResponse.IsSuccessStatusCode)
            {
                await _logger.LogUnsuccessfulHttpResponse(httpResponse);
                return FrozenDictionary<string, string?>.Empty;
            }

            var response = await httpResponse.Content.ReadFromJsonAsync<ExchangeTickersResponse>();
            if (!response!.Tickers.Any())
                break;
            allTickers.AddRange(response.Tickers);

            if (response.Tickers.Count() < MaxTickersPerRequest)
                break;
        }

        return Mapping.ToSymbolToIdMap(allTickers).ToFrozenDictionary();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AssetCoinGecko>> GetCoinsMarkets(IEnumerable<string> ids)
    {
        var idsArray = ids.ToArray();
        if (idsArray.Length == 0)
            return [];

        var chunks = idsArray.Chunk(MaxIdsPerRequest).Select(chunk => chunk.ToArray()).ToArray();
        var allResults = new List<AssetCoinGecko>();

        foreach (var chunk in chunks)
        {
            var chunkResult = await FetchMarketsForIds(chunk);
            if (chunk.Length != 0 && !chunkResult.Any())
                return [];
            allResults.AddRange(chunkResult);
        }

        return allResults;
    }

    private async Task<IEnumerable<AssetCoinGecko>> FetchMarketsForIds(string[] ids)
    {
        var endpoint = "/api/v3/coins/markets";
        endpoint += "?vs_currency=usd&per_page=" + MaxIdsPerRequest;
        endpoint += $"&ids={string.Join(",", ids)}";

        var httpResponse = await _httpClient.GetAsync(endpoint);
        if (!httpResponse.IsSuccessStatusCode)
        {
            await _logger.LogUnsuccessfulHttpResponse(httpResponse);
            return [];
        }

        var response = await httpResponse.Content.ReadFromJsonAsync<List<AssetCoinGecko>>();
        return response ?? [];
    }

    private static class Mapping
    {
        public static CoinCoinGecko ToCoinCoinGecko(CoinListResponse response) =>
            new()
            {
                Id = response.Id,
                Symbol = response.Symbol,
                Name = response.Name,
            };

        public static Dictionary<string, string?> ToSymbolToIdMap(List<TickerData> allTickers)
        {
            return allTickers
                .SelectMany(ticker =>
                    new[]
                    {
                        new { Symbol = ticker.BaseCoin, Id = ticker.BaseCoinId },
                        new { Symbol = ticker.TargetCoin, Id = ticker.TargetCoinId },
                    }
                )
                .GroupBy(item => item.Symbol, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group.FirstOrDefault(value => !string.IsNullOrEmpty(value.Id))?.Id,
                    StringComparer.OrdinalIgnoreCase
                );
        }
    }
}
