using FluentValidation;
using SharpGrip.FluentValidation.AutoValidation.Mvc.Extensions;
using SVC_Coins.ApiContracts.Requests.Validators.CoinCreation;
using SVC_Coins.ApiContracts.Requests.Validators.CoinMarketDataUpdate;
using SVC_Coins.ApiContracts.Requests.Validators.QuoteCoinCreation;
using SVC_Coins.ApiContracts.Requests.Validators.TradingPairCreation;

namespace SVC_Coins.Infrastructure.DependencyInjection;

/// <summary>
/// Extension methods for configuring validation services.
/// </summary>
public static class ValidationExtensions
{
    /// <summary>
    /// Adds validators and configures auto-validation.
    /// </summary>
    /// <param name="services">The IServiceCollection to add services to.</param>
    /// <returns>The updated IServiceCollection.</returns>
    public static IServiceCollection AddInputValidationServices(this IServiceCollection services)
    {
        // Register the API model validators from assemblies
        services.AddValidatorsFromAssemblyContaining<CoinCreationRequestsValidator>();
        services.AddValidatorsFromAssemblyContaining<CoinMarketDataUpdateRequestsValidator>();
        services.AddValidatorsFromAssemblyContaining<TradingPairCreationRequestsValidator>();
        services.AddValidatorsFromAssemblyContaining<QuoteCoinCreationRequestsValidator>();

        // Enable auto-validation for MVC controllers
        services.AddFluentValidationAutoValidation();

        return services;
    }
}
