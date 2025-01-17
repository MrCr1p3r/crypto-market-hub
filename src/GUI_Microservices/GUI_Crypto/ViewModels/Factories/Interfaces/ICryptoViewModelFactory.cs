using GUI_Crypto.Models.Chart;

namespace GUI_Crypto.ViewModels.Factories.Interfaces;

/// <summary>
/// Factory interface for creating view models related to cryptocurrency.
/// </summary>
public interface ICryptoViewModelFactory
{
    /// <summary>
    /// Creates an overview view model for displaying cryptocurrency data.
    /// </summary>
    /// <returns>Created overview view model.</returns>
    Task<OverviewViewModel> CreateOverviewViewModel();

    /// <summary>
    /// Creates a chart view model for displaying cryptocurrency chart data.
    /// </summary>
    /// <param name="coin">Contains data that will be used to fetch coin and kline data.</param>
    /// <returns>Created chart view model.</returns>
    Task<ChartViewModel> CreateChartViewModel(CoinChartRequest coin);
}
