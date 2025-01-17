using GUI_Crypto.Clients.Interfaces;
using GUI_Crypto.Models.Chart;
using GUI_Crypto.Models.Input;
using GUI_Crypto.Models.Output;
using GUI_Crypto.ViewModels.Factories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GUI_Crypto.Controllers;

/// <summary>
/// Controller for handling the chart page related operations.
/// </summary>
[Route("chart")]
public class ChartController(
    ICryptoViewModelFactory viewModelFactory,
    ISvcExternalClient svcExternalClient
) : Controller
{
    private readonly ICryptoViewModelFactory _viewModelFactory = viewModelFactory;
    private readonly ISvcExternalClient _svcExternalClient = svcExternalClient;

    /// <summary>
    /// Renders the Chart view for a specific coin.
    /// </summary>
    /// <param name="idCoin">The coin ID.</param>
    /// <returns>Rendered view.</returns>
    /// <response code="200">View rendered successfully.</response>
    [HttpPost("")]
    [Consumes("application/x-www-form-urlencoded")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> Chart([FromForm] CoinChartRequest coin)
    {
        var viewModel = await _viewModelFactory.CreateChartViewModel(coin);
        return View("Chart", viewModel);
    }

    /// <summary>
    /// Fetches Kline data for a specific request.
    /// </summary>
    /// <param name="request">The request parameters for fetching Kline data.</param>
    /// <returns>A collection of Kline data objects.</returns>
    [HttpGet("klines")]
    [ProducesResponseType(typeof(IEnumerable<KlineDataExchange>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetKlineData([FromQuery] KlineDataRequest request)
    {
        var klineData = await _svcExternalClient.GetKlineData(request);
        return Ok(klineData);
    }
}
