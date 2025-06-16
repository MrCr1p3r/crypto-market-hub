namespace SVC_External.Infrastructure.CoinGecko;

/// <summary>
/// Service for managing CoinGecko authentication state and API key fallback.
/// </summary>
public interface ICoinGeckoAuthenticationStateService
{
    /// <summary>
    /// Gets a value indicating whether the service is currently using API key authentication.
    /// </summary>
    bool IsUsingApiKey { get; }

    /// <summary>
    /// Gets the current API key if available and in use.
    /// </summary>
    string? CurrentApiKey { get; }

    /// <summary>
    /// Switches to basic authentication mode (without API key).
    /// This is called when the monthly API limit is reached.
    /// </summary>
    void SwitchToBasicMode();
}
