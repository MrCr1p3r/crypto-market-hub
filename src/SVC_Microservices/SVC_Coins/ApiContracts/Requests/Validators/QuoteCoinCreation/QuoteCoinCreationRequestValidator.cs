using FluentValidation;

namespace SVC_Coins.ApiContracts.Requests.Validators.QuoteCoinCreation;

/// <summary>
/// Defines validation rules for quote coin creation request model.
/// </summary>
public class QuoteCoinCreationRequestValidator : AbstractValidator<QuoteCoinCreationRequest>
{
    public QuoteCoinCreationRequestValidator()
    {
        RuleFor(request => request.Symbol)
            .NotEmpty()
            .WithMessage("Quote coin symbol is required.")
            .MaximumLength(50)
            .WithMessage("Quote coin symbol must not exceed 50 characters.")
            .Must(symbol => symbol.Equals(symbol.ToUpperInvariant(), StringComparison.Ordinal))
            .WithMessage("Quote coin symbol '{PropertyValue}' must be uppercase.");

        RuleFor(request => request.Name)
            .NotEmpty()
            .WithMessage("Quote coin name is required.")
            .MaximumLength(50)
            .WithMessage("Quote coin name must not exceed 50 characters.");

        RuleFor(request => request.Category)
            .IsInEnum()
            .WithMessage("Invalid coin category '{PropertyValue}'.")
            .When(request => request.Category.HasValue);

        RuleFor(request => request.IdCoinGecko)
            .NotEmpty()
            .WithMessage("Quote coin CoinGecko ID must not be an empty string.")
            .MaximumLength(100)
            .WithMessage("Quote coin CoinGecko ID must not exceed 100 characters.")
            .When(request => request.IdCoinGecko is not null);
    }
}
