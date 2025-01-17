using GUI_Crypto.Models.Chart;
using SharedLibrary.Enums;

namespace GUI_Crypto.ViewModels;

/// <summary>
/// View model for displaying cryptocurrency chart data.
/// </summary>
public class ChartViewModel
{
    /// <summary>
    /// The coin details.
    /// </summary>
    public required CoinChart Coin { get; init; }

    /// <summary>
    /// The available kline intervals.
    /// </summary>
    public IEnumerable<ExchangeKlineInterval> KlineIntervals { get; } =
        Enum.GetValues(typeof(ExchangeKlineInterval)).Cast<ExchangeKlineInterval>();
}
