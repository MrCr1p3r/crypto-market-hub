using SVC_Bridge.Clients.Interfaces;
using SVC_Bridge.Models.Input;

namespace SVC_Bridge.Clients;

/// <summary>
/// Implementation for interacting with the SVC_Kline microservice.
/// </summary>
public class SvcKlineClient(IHttpClientFactory httpClientFactory) : ISvcKlineClient
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("SvcKlineClient");

    /// <inheritdoc />
    public async Task ReplaceAllKlineData(IEnumerable<KlineDataNew> newKlineData) =>
        await _httpClient.PutAsJsonAsync("/api/klineData/replaceAll", newKlineData);
}
