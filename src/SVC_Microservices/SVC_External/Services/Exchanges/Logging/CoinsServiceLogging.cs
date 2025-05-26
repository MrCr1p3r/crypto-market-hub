namespace SVC_External.Services.Exchanges.Logging;

/// <summary>
/// Logging methods for the CoinsService class.
/// </summary>
public static partial class CoinsServiceLogging
{
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Could not find names for the following symbols in {Exchange}: {Symbols}"
    )]
    public static partial void LogSymbolsWithoutNames(
        this ILogger logger,
        string exchange,
        string symbols
    );

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Could not find names for the following quote symbols in {Exchange}: {Symbols}"
    )]
    public static partial void LogQuoteSymbolsWithoutNames(
        this ILogger logger,
        string exchange,
        string symbols
    );

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "The following coins from {Exchange} are inactive on CoinGecko: {Symbols}"
    )]
    public static partial void LogInactiveCoinGeckoCoins(
        this ILogger logger,
        string exchange,
        string symbols
    );
}
