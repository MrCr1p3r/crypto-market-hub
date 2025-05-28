using FluentResults;
using SharedLibrary.Extensions.HttpClient.Internal;
using SVC_Bridge.MicroserviceClients.SvcKline.Contracts.Requests;
using SVC_Bridge.MicroserviceClients.SvcKline.Contracts.Responses;

namespace SVC_Bridge.MicroserviceClients.SvcKline;

/// <summary>
/// Implementation for interacting with the SVC_Kline microservice.
/// </summary>
public class SvcKlineClient(IHttpClientFactory httpClientFactory, ILogger<SvcKlineClient> logger)
    : ISvcKlineClient
{
    private const string BaseUrl = "kline";
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("SvcKlineClient");

    /// <inheritdoc />
    public async Task<Result<IEnumerable<KlineDataResponse>>> ReplaceKlineData(
        IEnumerable<KlineDataCreationRequest> newKlineData
    ) =>
        await _httpClient.PutAsJsonSafeAsync<
            IEnumerable<KlineDataCreationRequest>,
            IEnumerable<KlineDataResponse>
        >(BaseUrl, newKlineData, logger, "Failed to replace kline data.");
}
