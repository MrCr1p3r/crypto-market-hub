using Microsoft.AspNetCore.Mvc;
using SharedLibrary.Extensions;
using SVC_Bridge.ApiContracts.Responses.Coins;
using SVC_Bridge.Services.Interfaces;

namespace SVC_Bridge.ApiControllers;

/// <summary>
/// Controller for handling trading pairs operations.
/// </summary>
[ApiController]
[Route("bridge/trading-pairs")]
public class TradingPairsController(ITradingPairsService tradingPairsService) : ControllerBase
{
    private readonly ITradingPairsService _tradingPairsService = tradingPairsService;

    /// <summary>
    /// Updates all trading pairs by synchronizing with external data sources.
    /// </summary>
    /// <returns>A collection of coins with updated trading pairs.</returns>
    /// <response code="200">Trading pairs were successfully updated.</response>
    /// <response code="500">Internal error occurred during trading pairs update operation.</response>
    [HttpPut]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IEnumerable<Coin>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateTradingPairs()
    {
        var result = await _tradingPairsService.UpdateTradingPairs();
        return result.ToActionResult(this);
    }
}
