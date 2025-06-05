namespace SharedLibrary.Enums;

/// <summary>
/// Represents time intervals for fetching and displaying kline data from exchanges.
/// Used to specify the granularity of data in trading or financial applications.
/// </summary>
public enum ExchangeKlineInterval
{
    /// <summary>
    /// One minute interval.
    /// </summary>
    OneMinute = 1,

    /// <summary>
    /// Five minutes interval.
    /// </summary>
    FiveMinutes = 5,

    /// <summary>
    /// Fifteen minutes interval.
    /// </summary>
    FifteenMinutes = 15,

    /// <summary>
    /// Thirty minutes interval.
    /// </summary>
    ThirtyMinutes = 30,

    /// <summary>
    /// One hour interval.
    /// </summary>
    OneHour = 60,

    /// <summary>
    /// Four hours interval.
    /// </summary>
    FourHours = 240,

    /// <summary>
    /// One day interval.
    /// </summary>
    OneDay = 1440,

    /// <summary>
    /// One week interval.
    /// </summary>
    OneWeek = 10080,

    /// <summary>
    /// One month interval.
    /// </summary>
    OneMonth = 43200,
}
