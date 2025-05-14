using FluentValidation;

namespace SVC_Coins.ApiContracts.Requests.Validators.CoinMarketDataUpdate;

/// <summary>
/// Defines validation rules for the coin market data update request model.
/// </summary>
public class CoinMarketDataUpdateRequestValidator : AbstractValidator<CoinMarketDataUpdateRequest>
{
    public CoinMarketDataUpdateRequestValidator()
    {
        RuleFor(request => request.Id)
            .GreaterThan(0)
            .WithMessage("Coin ID '{PropertyValue}' is not valid. ID must be greater than 0.");

        RuleFor(request => request.MarketCapUsd)
            .GreaterThanOrEqualTo(0)
            .WithMessage(request =>
                $"Market capitalization for coin '{request.Id}' cannot be negative."
            )
            .When(request => request.MarketCapUsd.HasValue);

        RuleFor(request => request.PriceUsd)
            .GreaterThanOrEqualTo(0)
            .WithMessage(request => $"Price for coin '{request.Id}' cannot be negative.")
            .When(request => request.PriceUsd.HasValue);
    }
}
