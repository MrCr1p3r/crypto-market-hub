using FluentValidation;

namespace SVC_Coins.ApiContracts.Requests.Validators.TradingPairCreation;

/// <summary>
/// Defines validation rules for a list of trading pair creation requests.
/// </summary>
public class TradingPairCreationRequestsValidator
    : AbstractValidator<List<TradingPairCreationRequest>>
{
    public TradingPairCreationRequestsValidator()
    {
        RuleFor(requests => requests)
            .NotEmpty()
            .WithMessage("At least one trading pair creation request must be provided.");

        RuleFor(requests => requests)
            .Custom(
                (requests, context) =>
                {
                    var duplicates = GetDuplicatePairs(requests);
                    if (duplicates.Any())
                    {
                        context.AddFailure(
                            $"Duplicate trading pairs found: {string.Join("; ", duplicates)}."
                        );
                    }
                }
            );

        RuleForEach(request => request).SetValidator(new TradingPairCreationRequestValidator());
    }

    private static IEnumerable<string> GetDuplicatePairs(
        IEnumerable<TradingPairCreationRequest> requests
    )
    {
        return requests
            .GroupBy(request => (request.IdCoinMain, request.IdCoinQuote))
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .Select(pair => $"Main coin ID: {pair.IdCoinMain}, Quote coin ID: {pair.IdCoinQuote}");
    }
}
