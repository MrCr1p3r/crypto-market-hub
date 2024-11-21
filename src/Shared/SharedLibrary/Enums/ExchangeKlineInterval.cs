namespace SharedLibrary.Enums;

/// <summary>
/// Represents time intervals for fetching and displaying kline data from exchanges.
/// Used to specify the granularity of data in trading or financial applications.
/// </summary>
public enum ExchangeKlineInterval
{
    OneMinute = 1,
    FiveMinutes = 5,
    FifteenMinutes = 15,
    ThirtyMinutes = 30,
    OneHour = 60,
    FourHours = 240,
    OneDay = 1440,
    OneWeek = 10080,
    OneMonth = 43200
}
