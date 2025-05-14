using FluentValidation;
using SVC_Coins.ApiContracts.Requests.CoinCreation;

namespace SVC_Coins.ApiContracts.Requests.Validators.CoinCreation;

/// <summary>
/// Defines validation rules for coin-creation model collection.
/// </summary>
public class CoinCreationRequestsValidator : AbstractValidator<List<CoinCreationRequest>>
{
    public CoinCreationRequestsValidator()
    {
        RuleFor(request => request)
            .NotEmpty()
            .WithMessage("At least one new coin should be provided.");

        RuleFor(requests => requests)
            .Custom(
                (requests, context) =>
                {
                    var duplicates = GetDuplicateSymbolNamePairs(requests);
                    if (duplicates.Any())
                    {
                        context.AddFailure(
                            $"Duplicate symbol-name pairs found: {string.Join(", ", duplicates)}."
                        );
                    }
                }
            );

        RuleForEach(request => request).SetValidator(new CoinCreationRequestValidator());
    }

    private static IEnumerable<string> GetDuplicateSymbolNamePairs(
        IEnumerable<CoinCreationRequest> requests
    ) =>
        requests
            .Where(r => r != null)
            .GroupBy(r => (r.Symbol, r.Name))
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .Select(d => $"{d.Name} ({d.Symbol})");
}
