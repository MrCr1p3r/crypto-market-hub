using FluentValidation;
using SVC_Coins.ApiContracts.Requests.CoinCreation;

namespace SVC_Coins.ApiContracts.Requests.Validators.CoinCreation;

/// <summary>
/// Defines validation rules for coin-creation model.
/// </summary>
public class CoinCreationRequestValidator : AbstractValidator<CoinCreationRequest>
{
    public CoinCreationRequestValidator()
    {
        RuleFor(request => request.Symbol)
            .NotEmpty()
            .WithMessage("Main coin symbol is required.")
            .MaximumLength(50)
            .WithMessage("Main coin symbol must not exceed 50 characters.")
            .Must(symbol => symbol.Equals(symbol.ToUpperInvariant(), StringComparison.Ordinal))
            .WithMessage("Main coin symbol '{PropertyValue}' must be uppercase.");

        RuleFor(request => request.Name)
            .NotEmpty()
            .WithMessage("Main coin name is required.")
            .MaximumLength(50)
            .WithMessage("Main coin name must not exceed 50 characters.");

        RuleFor(request => request.Category)
            .IsInEnum()
            .WithMessage("Invalid coin category '{PropertyValue}'.")
            .When(request => request.Category.HasValue);

        RuleFor(request => request.IdCoinGecko)
            .NotEmpty()
            .WithMessage("Main coin CoinGecko ID must not be an empty string.")
            .MaximumLength(100)
            .WithMessage("Main coin CoinGecko ID must not exceed 100 characters.")
            .When(request => request.IdCoinGecko is not null);

        RuleFor(request => request.TradingPairs)
            .NotEmpty()
            .WithMessage(request =>
                $"At least one trading pair is required for {request.Name} ({request.Symbol})."
            )
            .ForEach(pair => pair.SetValidator(new CoinCreationTradingPairValidator()));
    }
}
