using FluentResults;
using GUI_Crypto.MicroserviceClients.SvcCoins.Contracts.Requests.CoinCreation;
using GUI_Crypto.MicroserviceClients.SvcCoins.Contracts.Responses;
using SharedLibrary.Extensions.HttpClient.Internal;
using static SharedLibrary.Errors.GenericErrors;

namespace GUI_Crypto.MicroserviceClients.SvcCoins;

/// <summary>
/// Implements interactions with SVC_Coins microservice.
/// </summary>
public class SvcCoinsClient(IHttpClientFactory httpClientFactory, ILogger<SvcCoinsClient> logger)
    : ISvcCoinsClient
{
    private const string BaseUrl = "coins";
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("SvcCoinsClient");
    private readonly ILogger<SvcCoinsClient> _logger = logger;

    /// <inheritdoc />
    public async Task<Result<IEnumerable<Coin>>> GetAllCoins() =>
        await _httpClient.GetFromJsonSafeAsync<IEnumerable<Coin>>(
            BaseUrl,
            _logger,
            "Failed to get all coins."
        );

    /// <inheritdoc />
    public async Task<Result<Coin>> GetCoinById(int idCoin)
    {
        var result = await _httpClient.GetFromJsonSafeAsync<IEnumerable<Coin>>(
            $"{BaseUrl}?ids={idCoin}",
            _logger,
            $"Failed to get coin with ID {idCoin}."
        );

#pragma warning disable IDE0046 // Convert to conditional expression
        if (result.IsFailed)
        {
            return Result.Fail(result.Errors);
        }
#pragma warning restore IDE0046 // Convert to conditional expression

        return result.Value.Any()
            ? Result.Ok(result.Value.First())
            : Result.Fail(new NotFoundError($"Coin with ID {idCoin} not found."));
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<Coin>>> CreateCoins(
        IEnumerable<CoinCreationRequest> coins
    ) =>
        await _httpClient.PostAsJsonSafeAsync<IEnumerable<CoinCreationRequest>, IEnumerable<Coin>>(
            BaseUrl,
            coins,
            _logger,
            "Failed to create coins."
        );

    /// <inheritdoc />
    public async Task<Result> DeleteMainCoin(int idCoin) =>
        await _httpClient.DeleteSafeAsync($"{BaseUrl}/{idCoin}", _logger, "Failed to delete coin.");

    /// <inheritdoc />
    public async Task<Result> DeleteAllCoins() =>
        await _httpClient.DeleteSafeAsync(BaseUrl, _logger, "Failed to delete all coins.");
}
