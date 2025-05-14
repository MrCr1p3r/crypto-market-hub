using FluentValidation;

namespace SVC_Coins.ApiContracts.Requests.Validators.CoinMarketDataUpdate;

/// <summary>
/// Defines validation rules for a list of coin market data update requests.
/// </summary>
public class CoinMarketDataUpdateRequestsValidator
    : AbstractValidator<List<CoinMarketDataUpdateRequest>>
{
    public CoinMarketDataUpdateRequestsValidator()
    {
        RuleFor(requests => requests)
            .NotEmpty()
            .WithMessage("At least one coin market data update request should be provided.");

        RuleFor(requests => requests)
            .Custom(
                (requests, context) =>
                {
                    var duplicates = GetDuplicateIds(requests);
                    if (duplicates.Any())
                    {
                        context.AddFailure(
                            $"Duplicate coin IDs found: {string.Join(", ", duplicates)}."
                        );
                    }
                }
            );

        RuleForEach(request => request).SetValidator(new CoinMarketDataUpdateRequestValidator());
    }

    private static IEnumerable<int> GetDuplicateIds(
        IEnumerable<CoinMarketDataUpdateRequest> requests
    ) =>
        requests
            .GroupBy(request => request.Id)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key);
}
