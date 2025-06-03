using FluentResults;
using SharedLibrary.Extensions.HttpClient.Internal;
using SVC_Scheduler.MicroserviceClients.SvcExternal.Contracts.Responses.Coins;

namespace SVC_Scheduler.MicroserviceClients.SvcExternal;

/// <summary>
/// Implementation for interacting with the SVC_External microservice.
/// </summary>
public class SvcExternalClient(
    IHttpClientFactory httpClientFactory,
    ILogger<SvcExternalClient> logger
) : ISvcExternalClient
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("SvcExternalClient");

    /// <inheritdoc />
    public async Task<Result<IEnumerable<Coin>>> GetAllSpotCoins() =>
        await _httpClient.GetFromJsonSafeAsync<IEnumerable<Coin>>(
            "exchanges/coins/spot",
            logger,
            "Failed to retrieve all spot coins."
        );
}
