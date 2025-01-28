using GUI_Crypto.Clients.Interfaces;
using GUI_Crypto.Models.Input;

namespace GUI_Crypto.Clients;

/// <summary>
/// Implementation for interacting with the SVC_Bridge microservice.
/// </summary>
public class SvcBridgeClient(IHttpClientFactory httpClientFactory) : ISvcBridgeClient
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("SvcBridgeClient");

    /// <inheritdoc />
    public async Task UpdateEntireKlineData(KlineDataUpdateRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "/bridge/kline/updateEntireKlineData",
            request
        );
        response.EnsureSuccessStatusCode();
    }
}
