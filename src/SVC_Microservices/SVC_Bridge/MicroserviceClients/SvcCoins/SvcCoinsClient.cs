using FluentResults;
using SharedLibrary.Extensions.HttpClient.Internal;
using SVC_Bridge.MicroserviceClients.SvcCoins.Contracts.Requests;
using SVC_Bridge.MicroserviceClients.SvcCoins.Contracts.Responses;

namespace SVC_Bridge.MicroserviceClients.SvcCoins;

/// <summary>
/// Implements interactions with SVC_Coins microservice.
/// </summary>
public class SvcCoinsClient(IHttpClientFactory httpClientFactory, ILogger<SvcCoinsClient> logger)
    : ISvcCoinsClient
{
    private const string BaseUrl = "coins";
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("SvcCoinsClient");
    private readonly ILogger<SvcCoinsClient> _logger = logger;

    /// <inheritdoc />
    public async Task<Result<IEnumerable<Coin>>> GetAllCoins() =>
        await _httpClient.GetFromJsonSafeAsync<IEnumerable<Coin>>(
            BaseUrl,
            _logger,
            "Failed to get all coins."
        );

    /// <inheritdoc />
    public async Task<Result<IEnumerable<Coin>>> UpdateCoinsMarketData(
        IEnumerable<CoinMarketDataUpdateRequest> requests
    ) =>
        await _httpClient.PatchAsJsonSafeAsync<
            IEnumerable<CoinMarketDataUpdateRequest>,
            IEnumerable<Coin>
        >($"{BaseUrl}/market-data", requests, _logger, "Failed to update coins market data.");

    /// <inheritdoc />
    public async Task<Result<IEnumerable<TradingPairCoinQuote>>> CreateQuoteCoins(
        IEnumerable<QuoteCoinCreationRequest> quoteCoins
    ) =>
        await _httpClient.PostAsJsonSafeAsync<
            IEnumerable<QuoteCoinCreationRequest>,
            IEnumerable<TradingPairCoinQuote>
        >($"{BaseUrl}/quote", quoteCoins, _logger, "Failed to create quote coins.");

    /// <inheritdoc />
    public async Task<Result<IEnumerable<Coin>>> ReplaceTradingPairs(
        IEnumerable<TradingPairCreationRequest> requests
    ) =>
        await _httpClient.PutAsJsonSafeAsync<
            IEnumerable<TradingPairCreationRequest>,
            IEnumerable<Coin>
        >($"{BaseUrl}/trading-pairs", requests, _logger, "Failed to replace trading pairs.");

    /// <inheritdoc />
    public async Task<Result> DeleteUnreferencedCoins() =>
        await _httpClient.DeleteSafeAsync(
            $"{BaseUrl}/unreferenced",
            _logger,
            "Failed to delete unreferenced coins."
        );
}
