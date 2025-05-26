namespace SVC_External.Services.Exchanges.Logging;

/// <summary>
/// Logging methods for the KlineDataService class.
/// </summary>
public static partial class KlineDataServiceLogging
{
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
