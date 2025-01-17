using GUI_Crypto.Clients.Interfaces;
using GUI_Crypto.Models.Input;
using GUI_Crypto.Models.Output;

namespace GUI_Crypto.Clients;

/// <summary>
/// Implementation for interacting with the Kline Data Service API.
/// </summary>
public class SvcKlineClient(IHttpClientFactory httpClientFactory) : ISvcKlineClient
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("SvcKlineClient");
    private const string BaseUrl = "kline";

    /// <inheritdoc />
    public async Task InsertKlineData(KlineDataNew klineData) =>
        await _httpClient.PostAsJsonAsync($"{BaseUrl}/insert", klineData);

    /// <inheritdoc />
    public async Task InsertManyKlineData(IEnumerable<KlineDataNew> klineDataList) =>
        await _httpClient.PostAsJsonAsync($"{BaseUrl}/insertMany", klineDataList);

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<int, IEnumerable<KlineData>>> GetAllKlineData() =>
        await _httpClient.GetFromJsonAsync<IReadOnlyDictionary<int, IEnumerable<KlineData>>>(
            $"{BaseUrl}/all"
        ) ?? new Dictionary<int, IEnumerable<KlineData>>();

    /// <inheritdoc />
    public async Task DeleteKlineDataForTradingPair(int idTradePair) =>
        await _httpClient.DeleteAsync($"{BaseUrl}/{idTradePair}");
}
