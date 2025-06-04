using FluentResults;
using GUI_Crypto.MicroserviceClients.SvcExternal.Contracts.Requests;
using GUI_Crypto.MicroserviceClients.SvcExternal.Contracts.Responses.Coins;
using GUI_Crypto.MicroserviceClients.SvcExternal.Contracts.Responses.KlineData;
using SharedLibrary.Extensions.HttpClient.Internal;

namespace GUI_Crypto.MicroserviceClients.SvcExternal;

/// <summary>
/// Implementation for interacting with the SVC_External microservice.
/// </summary>
public class SvcExternalClient(
    IHttpClientFactory httpClientFactory,
    ILogger<SvcExternalClient> logger
) : ISvcExternalClient
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("SvcExternalClient");
    private readonly ILogger<SvcExternalClient> _logger = logger;

    /// <inheritdoc />
    public async Task<Result<IEnumerable<Coin>>> GetAllSpotCoins() =>
        await _httpClient.GetFromJsonSafeAsync<IEnumerable<Coin>>(
            "exchanges/coins/spot",
            _logger,
            "Failed to retrieve all spot coins."
        );

    /// <inheritdoc />
    public async Task<Result<KlineDataResponse>> GetKlineData(KlineDataRequest request) =>
        await _httpClient.PostAsJsonSafeAsync<KlineDataRequest, KlineDataResponse>(
            "exchanges/kline/query",
            request,
            _logger,
            "Failed to retrieve kline data."
        );
}
