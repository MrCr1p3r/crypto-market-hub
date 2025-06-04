using GUI_Crypto.ServiceModels;
using GUI_Crypto.ViewModels.Chart;

namespace GUI_Crypto.ViewModels;

/// <summary>
/// Defines methods for creating cryptocurrency related view models.
/// </summary>
public interface ICryptoViewModelFactory
{
    /// <summary>
    /// Creates a chart view model from aggregated data.
    /// </summary>
    /// <param name="data">Aggregated data for the chart view model.</param>
    /// <returns>Created chart view model.</returns>
    ChartViewModel CreateChartViewModel(ChartData data);
}
