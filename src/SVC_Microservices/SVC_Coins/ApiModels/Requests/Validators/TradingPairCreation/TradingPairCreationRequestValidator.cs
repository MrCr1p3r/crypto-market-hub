using FluentValidation;
using SharedLibrary.Enums;

namespace SVC_Coins.ApiModels.Requests.Validators.TradingPairCreation;

/// <summary>
/// Validator for a single trading pair creation request.
/// </summary>
public class TradingPairCreationRequestValidator : AbstractValidator<TradingPairCreationRequest>
{
    public TradingPairCreationRequestValidator()
    {
        RuleFor(request => request.IdCoinMain)
            .GreaterThan(0)
            .WithMessage("Coin ID '{PropertyValue}' must be greater than 0.");

        RuleFor(request => request.IdCoinQuote)
            .GreaterThan(0)
            .WithMessage("Quote coin ID '{PropertyValue}' must be greater than 0.");

        RuleFor(request => request.Exchanges)
            .NotEmpty()
            .WithMessage(request =>
                $"At least one exchange for the trading pair '{request.IdCoinMain} - {request.IdCoinQuote}' is required."
            )
            .ForEach(exchange =>
                exchange.IsInEnum().WithMessage("Invalid exchange '{PropertyValue}'.")
            );
        RuleFor(request => request)
            .Custom(
                (request, context) =>
                {
                    var duplicateExchanges = GetDuplicateExchanges(request.Exchanges);
                    if (duplicateExchanges.Any())
                    {
                        context.AddFailure(
                            $"Duplicate exchanges found for the trading pair '{request.IdCoinMain} - {request.IdCoinQuote}': {string.Join(", ", duplicateExchanges)}."
                        );
                    }
                }
            );
    }

    private static IEnumerable<Exchange> GetDuplicateExchanges(IEnumerable<Exchange> exchanges) =>
        exchanges
            .GroupBy(exchange => exchange)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key);
}
