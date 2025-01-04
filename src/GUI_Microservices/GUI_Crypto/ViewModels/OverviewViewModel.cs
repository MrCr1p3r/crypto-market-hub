using GUI_Crypto.Models.Overview;

namespace GUI_Crypto.ViewModels;

/// <summary>
/// View model for displaying cryptocurrency data.
/// </summary>
public class OverviewViewModel
{
    /// <summary>
    /// The list of coins to display.
    /// </summary>
    public IEnumerable<Coin> Coins { get; set; } = [];
}
