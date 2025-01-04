using System.Globalization;
using Cysharp.Web;
using SVC_Bridge.Clients.Interfaces;
using SVC_Bridge.Models.Input;
using SVC_Bridge.Models.Output;

namespace SVC_Bridge.Clients;

/// <summary>
/// Implementation for interacting with the SVC_External microservice.
/// </summary>
public class SvcExternalClient(IHttpClientFactory httpClientFactory) : ISvcExternalClient
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("SvcExternalClient");

    /// <inheritdoc />
    public async Task<IEnumerable<KlineData>> GetKlineData(KlineDataRequest request)
    {
        var options = new WebSerializerOptions(WebSerializerProvider.Default)
        {
            CultureInfo = CultureInfo.InvariantCulture,
        };
        var queryString = WebSerializer.ToQueryString(request, options);
        return await _httpClient.GetFromJsonAsync<IEnumerable<KlineData>>(
                $"/exchanges/klineData?{queryString}"
            ) ?? [];
    }
}
