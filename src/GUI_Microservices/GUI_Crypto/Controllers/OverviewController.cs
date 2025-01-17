using GUI_Crypto.Models.Overview;
using GUI_Crypto.ViewModels.Factories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GUI_Crypto.Controllers;

/// <summary>
/// Controller for handling the overview page related operations.
/// </summary>
public class OverviewController(ICryptoViewModelFactory viewModelFactory) : Controller
{
    private readonly ICryptoViewModelFactory _viewModelFactory = viewModelFactory;

    /// <summary>
    /// Renders the Overview view.
    /// </summary>
    /// <returns>Rendered view.</returns>
    /// <response code="200">View rendered successfully.</response>
    [HttpGet("overview")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> Overview()
    {
        var viewModel = await _viewModelFactory.CreateOverviewViewModel();

        return View("Overview", viewModel);
    }
}
