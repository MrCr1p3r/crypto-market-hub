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
    public async Task<IEnumerable<Coin>> GetAllCoins() =>
        await _httpClient.GetFromJsonAsync<IEnumerable<Coin>>(BaseUrl) ?? [];

    /// <inheritdoc />
    public async Task<Result<IEnumerable<Coin>>> UpdateCoinsMarketData(
        IEnumerable<CoinMarketDataUpdateRequest> requests
    ) =>
        await _httpClient.PatchAsJsonSafeAsync<
            IEnumerable<CoinMarketDataUpdateRequest>,
            IEnumerable<Coin>
        >($"{BaseUrl}/market-data", requests, _logger, "Failed to update coins market data.");
}
