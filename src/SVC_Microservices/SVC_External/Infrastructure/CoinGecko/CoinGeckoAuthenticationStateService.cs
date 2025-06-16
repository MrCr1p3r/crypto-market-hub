namespace SVC_External.Infrastructure.CoinGecko;

/// <summary>
/// Thread-safe service for managing CoinGecko authentication state and API key fallback.
/// </summary>
public partial class CoinGeckoAuthenticationStateService : ICoinGeckoAuthenticationStateService
{
    private readonly object _lock = new();
    private readonly string? _configuredApiKey;
    private readonly ILogger<CoinGeckoAuthenticationStateService> _logger;
    private bool _isUsingApiKey;

    public CoinGeckoAuthenticationStateService(
        IConfiguration configuration,
        ILogger<CoinGeckoAuthenticationStateService> logger
    )
    {
        _configuredApiKey = configuration["COINGECKO_API_KEY"];
        _logger = logger;
        _isUsingApiKey = !string.IsNullOrEmpty(_configuredApiKey);

        Logging.LogAuthenticationInitialized(
            _logger,
            _isUsingApiKey ? "API Key" : "Basic",
            !string.IsNullOrEmpty(_configuredApiKey)
        );
    }

    /// <inheritdoc />
    public bool IsUsingApiKey
    {
        get
        {
            lock (_lock)
            {
                return _isUsingApiKey;
            }
        }
    }

    /// <inheritdoc />
    public string? CurrentApiKey
    {
        get
        {
            lock (_lock)
            {
                return _isUsingApiKey ? _configuredApiKey : null;
            }
        }
    }

    /// <inheritdoc />
    public void SwitchToBasicMode()
    {
        lock (_lock)
        {
            if (_isUsingApiKey)
            {
                _isUsingApiKey = false;

                Logging.LogAuthenticationSwitched(
                    _logger,
                    "API Key",
                    "Basic",
                    "Monthly limit reached"
                );
            }
        }
    }

    private static partial class Logging
    {
        [LoggerMessage(
            EventId = 2001,
            Level = LogLevel.Information,
            Message = "CoinGecko authentication initialized in {Mode} mode. API Key configured: {HasApiKey}"
        )]
        public static partial void LogAuthenticationInitialized(
            ILogger logger,
            string mode,
            bool hasApiKey
        );

        [LoggerMessage(
            EventId = 2002,
            Level = LogLevel.Warning,
            Message = "CoinGecko authentication switched from {FromMode} to {ToMode} mode. Reason: {Reason}"
        )]
        public static partial void LogAuthenticationSwitched(
            ILogger logger,
            string fromMode,
            string toMode,
            string reason
        );
    }
}
