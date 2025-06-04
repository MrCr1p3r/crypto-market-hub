using GUI_Crypto.ViewModels.Chart.Models;
using SharedLibrary.Enums;

namespace GUI_Crypto.ViewModels.Chart;

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
