using FluentResults;
using SharedLibrary.Extensions.HttpClient.Internal;
using SVC_Bridge.MicroserviceClients.SvcExternal.Contracts.Requests;
using SVC_Bridge.MicroserviceClients.SvcExternal.Contracts.Responses;
using SVC_Bridge.MicroserviceClients.SvcExternal.Contracts.Responses.KlineData;

namespace SVC_Bridge.MicroserviceClients.SvcExternal;

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
    public async Task<Result<IEnumerable<CoinGeckoAssetInfo>>> GetCoinGeckoAssetsInfo(
        IEnumerable<string> coinGeckoIds
    )
    {
        var queryString = string.Join("&", coinGeckoIds.Select(id => $"coinGeckoIds={id}"));
        return await _httpClient.GetFromJsonSafeAsync<IEnumerable<CoinGeckoAssetInfo>>(
            $"market-data-providers/coingecko/assets-info?{queryString}",
            logger,
            "Failed to retrieve CoinGecko assets info."
        );
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<KlineDataResponse>>> GetKlineData(
        KlineDataBatchRequest request
    ) =>
        await _httpClient.PostAsJsonSafeAsync<
            KlineDataBatchRequest,
            IEnumerable<KlineDataResponse>
        >("exchanges/kline/query/bulk", request, logger, "Failed to retrieve kline data.");
}
