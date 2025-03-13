using System.Collections.Frozen;
using FluentResults;
using SharedLibrary.Extensions;
using SVC_External.Clients.MarketDataProviders.Interfaces;
using SVC_External.Models.MarketDataProviders.Output;
using static SharedLibrary.Errors.GenericErrors;
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
    public async Task<Result<IEnumerable<CoinCoinGecko>>> GetCoinsList()
    {
        var endpoint = "/api/v3/coins/list";
        var response = await _httpClient.GetFromJsonSafeAsync<IEnumerable<CoinListResponse>>(
            endpoint,
            _logger,
            "Failed to retrieve coins list from CoinGecko"
        );

        return response.IsSuccess
            ? Result.Ok(response.Value.Select(Mapping.ToCoinCoinGecko))
            : Result.Fail(response.Errors);
    }

    /// <inheritdoc />
    public async Task<Result<FrozenDictionary<string, string?>>> GetSymbolToIdMapForExchange(
        string idExchange
    )
    {
        var allTickers = new List<TickerData>();
        var page = 1;
        while (true)
        {
            var endpoint =
                $"/api/v3/exchanges/{idExchange}/tickers?depth=false&order=volume_desc&page={page}";
            var response = await _httpClient.GetFromJsonSafeAsync<ExchangeTickersResponse>(
                endpoint,
                _logger,
                $"Failed to retrieve exchange tickers from CoinGecko for exchange: {idExchange}, page: {page}"
            );
            if (response.IsFailed)
                return Result.Fail(response.Errors);

            var tickers = response.Value.Tickers;
            if (tickers == null || !tickers.Any())
                break;

            allTickers.AddRange(tickers);

            if (tickers.Count() < MaxTickersPerRequest)
                break;

            page++;
        }

        return Result.Ok(Mapping.ToSymbolToIdMap(allTickers).ToFrozenDictionary());
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<AssetCoinGecko>>> GetMarketDataForCoins(
        IEnumerable<string> ids
    )
    {
        var idsArray = ids.ToArray();
        if (idsArray.Length == 0)
            return Result.Fail(new BadRequestError("No CoinGecko IDs provided"));

        var chunks = idsArray.Chunk(MaxIdsPerRequest).Select(chunk => chunk.ToArray()).ToArray();
        var allResults = new List<AssetCoinGecko>();

        foreach (var chunk in chunks)
        {
            var chunkResult = await FetchMarketDataForIds(chunk);
            if (chunkResult.IsFailed)
                return chunkResult;
            allResults.AddRange(chunkResult.Value);
        }

        return allResults;
    }

    private async Task<Result<IEnumerable<AssetCoinGecko>>> FetchMarketDataForIds(string[] ids)
    {
        var endpoint = "/api/v3/coins/markets";
        endpoint += "?vs_currency=usd&per_page=" + MaxIdsPerRequest;
        endpoint += $"&ids={string.Join(",", ids)}";
        var response = await _httpClient.GetFromJsonSafeAsync<IEnumerable<AssetCoinGecko>>(
            endpoint,
            _logger,
            $"Failed to fetch market data for following CoinGecko IDs: {string.Join(", ", ids)}"
        );

        return response.IsSuccess ? Result.Ok(response.Value) : Result.Fail(response.Errors);
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<string>>> GetStablecoinsIds()
    {
        var stablecoinsIds = new List<string>();
        var page = 1;
        while (true)
        {
            var endpoint = $"/api/v3/coins/markets";
            endpoint += "?vs_currency=usd";
            endpoint += "&category=stablecoins";
            endpoint += "&per_page=" + MaxIdsPerRequest;
            endpoint += "&page=" + page;
            endpoint += "&sparkline=false";
            var response = await _httpClient.GetFromJsonSafeAsync<List<AssetCoinGecko>>(
                endpoint,
                _logger,
                "Failed to fetch stablecoins from CoinGecko"
            );
            if (response.IsFailed)
                return Result.Fail(response.Errors);

            var coins = response.Value;
            if (coins == null || coins.Count == 0)
                break;

            stablecoinsIds.AddRange(coins.Select(coin => coin.Id));

            if (coins.Count < MaxIdsPerRequest)
                break;

            page++;
        }
        return Result.Ok<IEnumerable<string>>(stablecoinsIds);
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
