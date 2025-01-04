using GUI_Crypto.Clients.Interfaces;
using GUI_Crypto.Models.Input;
using GUI_Crypto.Models.Output;

namespace GUI_Crypto.Clients;

/// <summary>
/// Implements interactions with SVC_Coins microservice.
/// </summary>
public class SvcCoinsClient(IHttpClientFactory httpClientFactory) : ISvcCoinsClient
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("SvcCoinsClient");
    private const string BaseUrl = "coins";

    /// <inheritdoc />
    public async Task CreateCoin(CoinNew coin) =>
        await _httpClient.PostAsJsonAsync($"{BaseUrl}/insert", coin);

    /// <inheritdoc />
    public async Task<IEnumerable<Coin>> GetAllCoins() =>
        await _httpClient.GetFromJsonAsync<IEnumerable<Coin>>($"{BaseUrl}/all") ?? [];

    /// <inheritdoc />
    public async Task DeleteCoin(int idCoin) =>
        await _httpClient.DeleteAsync($"{BaseUrl}/{idCoin}");

    /// <inheritdoc />
    public async Task<int> CreateTradingPair(TradingPairNew tradingPair) =>
        await _httpClient
            .PostAsJsonAsync($"{BaseUrl}/tradingPairs/insert", tradingPair)
            .Result.Content.ReadFromJsonAsync<int>();
}
