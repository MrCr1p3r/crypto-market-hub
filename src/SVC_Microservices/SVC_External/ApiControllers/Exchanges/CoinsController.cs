using Microsoft.AspNetCore.Mvc;
using SharedLibrary.Extensions;
using SVC_External.ApiContracts.Responses.Exchanges.Coins;
using SVC_External.Services.Exchanges.Interfaces;

namespace SVC_External.ApiControllers.Exchanges;

/// <summary>
/// Controller for handling exchanges coins operations.
/// </summary>
[ApiController]
[Route("exchanges/coins")]
public class CoinsController(ICoinsService coinsService) : ControllerBase
{
    private readonly ICoinsService _coinsService = coinsService;

    /// <summary>
    /// Retrieves all spot coins from all available exchanges.
    /// </summary>
    /// <returns>Collection of coins, listen on all of the available exchanges.</returns>
    /// <response code="200">Returns the list of coins.</response>
    /// <response code="500">If something went wrong during coins retrieval.</response>
    [HttpGet("spot")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IEnumerable<Coin>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllSpotCoins()
    {
        var coins = await _coinsService.GetAllCurrentActiveSpotCoins();
        return coins.ToActionResult(this);
    }
}
