using Microsoft.AspNetCore.Mvc;
using SVC_External.DataCollectors.Interfaces;
using SVC_External.Models.Input;
using SVC_External.Models.Output;

namespace SVC_External.Controllers;

/// <summary>
/// Controller for handling exchanges operations.
/// </summary>
/// <param name="dataCollector">The exchanges data collector.</param>
[ApiController]
[Route("api/[controller]")]
public class ExchangesController(IExchangesDataCollector dataCollector) : ControllerBase
{
    private readonly IExchangesDataCollector _dataCollector = dataCollector;

    /// <summary>
    /// Fetches Kline (candlestick) data from available exchanges.
    /// </summary>
    /// <param name="request">The request parameters for fetching Kline data.</param>
    /// <returns>A collection of Kline data objects.</returns>
    [HttpGet("klineData")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IEnumerable<KlineData>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetKlineData([FromQuery] KlineDataRequest request)
    {
        var klineData = await _dataCollector.GetKlineData(request);
        return Ok(klineData);
    }

    /// <summary>
    /// Retrieves all listed coins from all available exchanges.
    /// </summary>
    /// <returns>A collection of coin symbols.</returns>
    [HttpGet("allListedCoins")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllListedCoins()
    {
        var coins = await _dataCollector.GetAllListedCoins();
        return Ok(coins);
    }
}
