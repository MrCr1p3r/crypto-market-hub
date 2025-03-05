namespace SVC_External.DataCollectors.Logging;

public static partial class ExchangesDataCollectorLogging
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

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "No kline data was found for coin with id {IdCoin} - {CoinSymbol} ({CoinName})."
    )]
    public static partial void LogNoKlineDataFoundForCoin(
        this ILogger logger,
        int idCoin,
        string coinSymbol,
        string coinName
    );
}
