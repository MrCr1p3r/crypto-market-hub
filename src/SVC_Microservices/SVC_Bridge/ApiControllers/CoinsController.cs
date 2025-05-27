using Microsoft.AspNetCore.Mvc;
using SharedLibrary.Extensions;
using SVC_Bridge.ApiContracts.Responses;
using SVC_Bridge.Services.Interfaces;

namespace SVC_Bridge.ApiControllers;

/// <summary>
/// Controller for handling coins operations.
/// </summary>
[ApiController]
[Route("bridge/coins")]
public class CoinsController(ICoinsService coinsService) : ControllerBase
{
    private readonly ICoinsService _coinsService = coinsService;

    /// <summary>
    /// Updates the market data for all coins in the system by fetching the latest data from external sources.
    /// </summary>
    /// <returns>A collection of updated coin market data.</returns>
    /// <response code="200">Market data successfully updated for all coins.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="500">Internal error occurred during market data update operation.</response>
    [HttpPost("market-data")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IEnumerable<CoinMarketData>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateCoinsMarketData()
    {
        var result = await _coinsService.UpdateCoinsMarketData();
        return result.ToActionResult(this);
    }
}
