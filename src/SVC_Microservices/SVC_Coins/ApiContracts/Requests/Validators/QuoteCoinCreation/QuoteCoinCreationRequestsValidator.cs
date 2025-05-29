using FluentValidation;

namespace SVC_Coins.ApiContracts.Requests.Validators.QuoteCoinCreation;

/// <summary>
/// Defines validation rules for quote coin creation request collection.
/// </summary>
public class QuoteCoinCreationRequestsValidator : AbstractValidator<List<QuoteCoinCreationRequest>>
{
    public QuoteCoinCreationRequestsValidator()
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

        RuleForEach(request => request).SetValidator(new QuoteCoinCreationRequestValidator());
    }

    private static IEnumerable<string> GetDuplicateSymbolNamePairs(
        IEnumerable<QuoteCoinCreationRequest> requests
    ) =>
        requests
            .Where(r => r != null)
            .GroupBy(r => (r.Symbol, r.Name))
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .Select(d => $"{d.Name} ({d.Symbol})");
}
