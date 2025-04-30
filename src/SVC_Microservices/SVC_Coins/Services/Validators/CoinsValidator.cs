using FluentResults;
using SharedLibrary.Errors;
using SVC_Coins.ApiModels.Requests;
using SVC_Coins.ApiModels.Requests.CoinCreation;
using SVC_Coins.Domain.ValueObjects;
using SVC_Coins.Repositories.Interfaces;
using SVC_Coins.Services.Validators.Interfaces;

namespace SVC_Coins.Services.Validators;

/// <inheritdoc/>
/// <summary>
/// Validator for coin-related data.
/// </summary>
public class CoinsValidator(
    ICoinsRepository coinsRepository,
    IExchangesRepository exchangesRepository
) : ICoinsValidator
{
    private readonly ICoinsRepository _coinsRepository = coinsRepository;
    private readonly IExchangesRepository _exchangesRepository = exchangesRepository;

    public async Task<Result> ValidateCoinCreationRequests(
        IEnumerable<CoinCreationRequest> requests
    )
    {
        var errorMessages = new List<string>();

        // Main and quote coins duplicates validation
        var duplicateCoins = await GetDuplicateCoins(requests);
        if (duplicateCoins.Any())
        {
            errorMessages.Add(
                $"The following coins already exist in the database: {string.Join(", ", duplicateCoins)}"
            );
        }

        // Non existing quote coins validation
        var nonExistingQuoteCoins = await GetNonExistingQuoteCoinIds(requests);
        if (nonExistingQuoteCoins.Any())
        {
            errorMessages.Add(
                $"Coins with following IDs do not exist in the database: {string.Join(", ", nonExistingQuoteCoins)}"
            );
        }

        // Trading pairs exchanges validation
        var invalidExchanges = await GetInvalidExchanges(requests);
        if (invalidExchanges.Any())
        {
            errorMessages.Add(
                $"The following exchanges do not exist in the database: {string.Join(", ", invalidExchanges)}"
            );
        }

        return errorMessages.Count > 0
            ? Result.Fail(new GenericErrors.BadRequestError(string.Join("\n\n", errorMessages)))
            : Result.Ok();
    }

    private async Task<IEnumerable<string>> GetDuplicateCoins(
        IEnumerable<CoinCreationRequest> requests
    )
    {
        var pairs = GetSymbolNamePairs(requests);

        var duplicateCoins = await _coinsRepository.GetCoinsBySymbolNamePairs(pairs);

        return duplicateCoins.Select(coin => $"{coin.Name} ({coin.Symbol})");
    }

    private static HashSet<CoinSymbolNamePair> GetSymbolNamePairs(
        IEnumerable<CoinCreationRequest> requests
    )
    {
        var requestsArray = requests.ToArray();

        var main = requestsArray.Select(Mapping.ToCoinSymbolNamePair);
        var quotes = requestsArray
            .SelectMany(r => r.TradingPairs)
            .Select(tp => tp.CoinQuote)
            .Where(q => q.Id is null)
            .Select(Mapping.ToCoinSymbolNamePair);

        return [.. main, .. quotes];
    }

    private async Task<IEnumerable<int>> GetNonExistingQuoteCoinIds(
        IEnumerable<CoinCreationRequest> requests
    )
    {
        var quoteCoinsIds = requests
            .SelectMany(request => request.TradingPairs)
            .Select(tradingPair => tradingPair.CoinQuote.Id)
            .OfType<int>()
            .ToHashSet();

        var missingCoinIds = await _coinsRepository.GetMissingCoinIds(quoteCoinsIds);
        return missingCoinIds;
    }

    private async Task<IEnumerable<SharedLibrary.Enums.Exchange>> GetInvalidExchanges(
        IEnumerable<CoinCreationRequest> requests
    )
    {
        var exchanges = await _exchangesRepository.GetAllExchanges();
        var validExchangeIds = exchanges.Select(exchange => exchange.Id).ToHashSet();
        var tradingPairsExchanges = requests
            .SelectMany(request => request.TradingPairs)
            .SelectMany(tradingPair => tradingPair.Exchanges)
            .ToHashSet();

        return tradingPairsExchanges.Where(exchange => !validExchangeIds.Contains((int)exchange));
    }

    public async Task<Result> ValidateMarketDataUpdateRequests(
        IEnumerable<CoinMarketDataUpdateRequest> requests
    )
    {
        var coinIds = requests.Select(request => request.Id).ToHashSet();
        var missingCoinIds = await _coinsRepository.GetMissingCoinIds(coinIds);

        return missingCoinIds.Count != 0
            ? Result.Fail(
                new GenericErrors.NotFoundError(
                    $"Coins with following IDs do not exist in the database: {string.Join(", ", missingCoinIds)}"
                )
            )
            : Result.Ok();
    }

    private static class Mapping
    {
        public static CoinSymbolNamePair ToCoinSymbolNamePair(CoinCreationRequest coin) =>
            new() { Symbol = coin.Symbol, Name = coin.Name };

        public static CoinSymbolNamePair ToCoinSymbolNamePair(CoinCreationCoinQuote coinQuote) =>
            new() { Symbol = coinQuote.Symbol, Name = coinQuote.Name };
    }
}
