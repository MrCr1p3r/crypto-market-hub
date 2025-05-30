using FluentResults;
using SharedLibrary.Extensions.HttpClient.Internal;
using SVC_Scheduler.SvcBridgeClient.Responses;
using SVC_Scheduler.SvcBridgeClient.Responses.Coins;
using SVC_Scheduler.SvcBridgeClient.Responses.KlineData;

namespace SVC_Scheduler.SvcBridgeClient;

/// <summary>
/// Implements interactions with SVC_Bridge microservice.
/// </summary>
public class SvcBridgeClient(IHttpClientFactory httpClientFactory, ILogger<SvcBridgeClient> logger)
    : ISvcBridgeClient
{
    private const string BaseUrl = "bridge";
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("SvcBridgeClient");
    private readonly ILogger<SvcBridgeClient> _logger = logger;

    /// <inheritdoc />
    public async Task<Result<IEnumerable<CoinMarketData>>> UpdateCoinsMarketData() =>
        await _httpClient.PostSafeAsync<IEnumerable<CoinMarketData>>(
            $"{BaseUrl}/coins/market-data",
            _logger,
            "Failed to update coins market data."
        );

    /// <inheritdoc />
    public async Task<Result<IEnumerable<KlineDataResponse>>> UpdateKlineData() =>
        await _httpClient.PostSafeAsync<IEnumerable<KlineDataResponse>>(
            $"{BaseUrl}/kline",
            _logger,
            "Failed to update kline data."
        );

    /// <inheritdoc />
    public async Task<Result<IEnumerable<Coin>>> UpdateTradingPairs() =>
        await _httpClient.PostSafeAsync<IEnumerable<Coin>>(
            $"{BaseUrl}/trading-pairs",
            _logger,
            "Failed to update trading pairs."
        );
}
