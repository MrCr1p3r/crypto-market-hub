using FluentResults;
using SharedLibrary.Errors;
using SVC_Coins.ApiContracts.Requests;
using SVC_Coins.Repositories.Interfaces;
using SVC_Coins.Services.Validators.Interfaces;

namespace SVC_Coins.Services.Validators;

/// <inheritdoc/>
/// <summary>
/// Validator for trading pair-related operations.
/// </summary>
public class TradingPairsValidator(
    ICoinsRepository coinsRepository,
    IExchangesRepository exchangesRepository
) : ITradingPairsValidator
{
    private readonly ICoinsRepository _coinsRepository = coinsRepository;
    private readonly IExchangesRepository _exchangesRepository = exchangesRepository;

    public async Task<Result> ValidateTradingPairsReplacementRequests(
        IEnumerable<TradingPairCreationRequest> requests
    )
    {
        var errorMessages = new List<string>();

        var missingCoinIds = await GetMissingCoinIds(requests);
        if (missingCoinIds.Any())
        {
            errorMessages.Add(
                $"Coins with following IDs do not exist in the database: {string.Join(", ", missingCoinIds)}"
            );
        }

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

    private async Task<IEnumerable<int>> GetMissingCoinIds(
        IEnumerable<TradingPairCreationRequest> newTradingPairs
    )
    {
        var coinIds = newTradingPairs
            .SelectMany(request => new[] { request.IdCoinMain, request.IdCoinQuote })
            .ToHashSet();
        var missingCoinIds = await _coinsRepository.GetMissingCoinIds(coinIds);
        return missingCoinIds;
    }

    private async Task<IEnumerable<SharedLibrary.Enums.Exchange>> GetInvalidExchanges(
        IEnumerable<TradingPairCreationRequest> requests
    )
    {
        var exchanges = await _exchangesRepository.GetAllExchanges();
        var validExchangeIds = exchanges.Select(exchange => exchange.Id).ToHashSet();
        var invalidExchanges = requests
            .SelectMany(request => request.Exchanges)
            .Where(exchange => !validExchangeIds.Contains((int)exchange));

        return invalidExchanges;
    }
}
