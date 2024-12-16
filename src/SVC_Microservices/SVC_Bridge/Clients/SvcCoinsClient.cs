using SVC_Bridge.Clients.Interfaces;
using SVC_Bridge.Models.Input;
using SVC_Bridge.Models.Output;

namespace SVC_Bridge.Clients;

/// <summary>
/// Implements interactions with SVC_Coins microservice.
/// </summary>
public class SvcCoinsClient(IHttpClientFactory httpClientFactory) : ISvcCoinsClient
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("SvcCoinsClient");

    /// <inheritdoc />
    public async Task<IEnumerable<Coin>> GetAllCoins() =>
        await _httpClient.GetFromJsonAsync<IEnumerable<Coin>>("/api/coins/getAll") ?? [];

    /// <inheritdoc />
    public async Task<int> InsertTradingPair(TradingPairNew tradingPair) =>
        await _httpClient
            .PostAsJsonAsync("/api/coins/tradingPair/insert", tradingPair)
            .Result.Content.ReadFromJsonAsync<int>();

    /// <inheritdoc />
    public async Task<IEnumerable<Coin>> GetQuoteCoinsPrioritized() =>
        await _httpClient.GetFromJsonAsync<IEnumerable<Coin>>("/api/coins/getQuoteCoinsPrioritized")
        ?? [];
}
