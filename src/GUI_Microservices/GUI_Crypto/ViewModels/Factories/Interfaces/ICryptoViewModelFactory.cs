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
}
