using System.Globalization;
using Cysharp.Web;
using GUI_Crypto.Clients.Interfaces;
using GUI_Crypto.Models.Input;
using GUI_Crypto.Models.Output;

namespace GUI_Crypto.Clients;

/// <summary>
/// Implementation for interacting with the External Service API.
/// </summary>
public class SvcExternalClient(IHttpClientFactory httpClientFactory) : ISvcExternalClient
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("SvcExternalClient");
    private const string BaseUrl = "exchanges";

    /// <inheritdoc />
    public async Task<IEnumerable<KlineDataExchange>> GetKlineData(KlineDataRequest request)
    {
        var options = new WebSerializerOptions(WebSerializerProvider.Default)
        {
            CultureInfo = CultureInfo.InvariantCulture,
        };
        var queryString = WebSerializer.ToQueryString(request, options);

        return await _httpClient.GetFromJsonAsync<IEnumerable<KlineDataExchange>>(
                $"{BaseUrl}/klineData?{queryString}"
            ) ?? [];
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetAllListedCoins() =>
        await _httpClient.GetFromJsonAsync<IEnumerable<string>>($"{BaseUrl}/allListedCoins") ?? [];
}
