using GUI_Crypto.ApiContracts.Requests.KlineData;
using GUI_Crypto.ApiContracts.Responses;
using GUI_Crypto.Services.Chart;
using GUI_Crypto.ViewModels;
using Microsoft.AspNetCore.Mvc;
using SharedLibrary.Extensions;

namespace GUI_Crypto.ApiControllers;

/// <summary>
/// Controller for handling the chart page related operations.
/// </summary>
[Route("chart")]
public class ChartController(IChartService chartService, ICryptoViewModelFactory viewModelFactory)
    : Controller
{
    private readonly IChartService _chartService = chartService;
    private readonly ICryptoViewModelFactory _viewModelFactory = viewModelFactory;

    /// <summary>
    /// Renders the chart view for a specific coin.
    /// </summary>
    /// <param name="idCoin">The id of the coin to fetch chart data for.</param>
    /// <param name="idTradingPair">The id of the trading pair to fetch chart data for.</param>
    /// <returns>Rendered view.</returns>
    /// <response code="200">View rendered successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="404">Coin or trading pair with the specified id was not found.</response>
    /// <response code="500">Internal server error occured.</response>
    [HttpGet("{idCoin:int}/{idTradingPair:int}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Chart([FromRoute] int idCoin, [FromRoute] int idTradingPair)
    {
        var chartData = await _chartService.GetChartData(idCoin, idTradingPair);
        if (chartData.IsFailed)
        {
            return chartData.ToActionResult(this);
        }

        var viewModel = _viewModelFactory.CreateChartViewModel(chartData.Value);
        return View("Chart", viewModel);
    }

    /// <summary>
    /// Fetches kline data for a specific trading pair.
    /// </summary>
    /// <param name="request">The request parameters for fetching kline data.</param>
    /// <returns>A collection of kline data objects.</returns>
    /// <response code="200">Kline data fetched successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="500">Internal server error occured while fetching kline data.</response>
    [HttpPost("klines/query")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(IEnumerable<Kline>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetKlineData([FromBody] KlineDataRequest request)
    {
        var klineData = await _chartService.GetKlineData(request);
        return klineData.ToActionResult(this);
    }
}
