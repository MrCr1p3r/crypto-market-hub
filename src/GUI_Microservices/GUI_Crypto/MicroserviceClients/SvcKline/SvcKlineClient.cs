using FluentResults;
using GUI_Crypto.MicroserviceClients.SvcKline.Contracts.Responses;
using SharedLibrary.Extensions.HttpClient.Internal;

namespace GUI_Crypto.MicroserviceClients.SvcKline;

/// <summary>
/// Implementation for interacting with the SVC_Kline microservice.
/// </summary>
public class SvcKlineClient(IHttpClientFactory httpClientFactory, ILogger<SvcKlineClient> logger)
    : ISvcKlineClient
{
    private const string BaseUrl = "kline";
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("SvcKlineClient");
    private readonly ILogger<SvcKlineClient> _logger = logger;

    /// <inheritdoc />
    public async Task<Result<IEnumerable<KlineDataResponse>>> GetAllKlineData() =>
        await _httpClient.GetFromJsonSafeAsync<IEnumerable<KlineDataResponse>>(
            BaseUrl,
            _logger,
            "Failed to get all kline data."
        );
}
