using FluentValidation;
using SVC_Coins.ApiContracts.Requests.CoinCreation;

namespace SVC_Coins.ApiContracts.Requests.Validators.CoinCreation;

/// <summary>
/// Validator for a trading pair during coin creation.
/// </summary>
public class CoinCreationTradingPairValidator : AbstractValidator<CoinCreationTradingPair>
{
    public CoinCreationTradingPairValidator()
    {
        RuleFor(p => p.CoinQuote)
            .NotNull()
            .WithMessage("Quote coin is required.")
            .SetValidator(new CoinCreationCoinQuoteValidator());

        RuleFor(p => p.Exchanges)
            .NotEmpty()
            .WithMessage("At least one exchange is required for each trading pair.")
            .ForEach(exchange =>
                exchange.IsInEnum().WithMessage("Invalid exchange '{PropertyValue}'.")
            );
    }
}
