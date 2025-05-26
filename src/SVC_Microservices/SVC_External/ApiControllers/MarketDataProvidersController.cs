using FluentResults;
using Microsoft.AspNetCore.Mvc;
using SharedLibrary.Errors;
using SharedLibrary.Extensions;
using SVC_External.ApiContracts.Responses.MarketDataProviders;
using SVC_External.Services.MarketDataProviders.Interfaces;

namespace SVC_External.ApiControllers;

/// <summary>
/// Controller for handling market data provider operations.
/// </summary>
[ApiController]
[Route("market-data-providers")]
public class MarketDataProvidersController(ICoinGeckoService coinGeckoService) : ControllerBase
{
    private readonly ICoinGeckoService _coinGeckoService = coinGeckoService;

    /// <summary>
    /// Retrieves assets info for specified coin IDs from CoinGecko.
    /// </summary>
    /// <param name="coinGeckoIds">Array of CoinGecko coin IDs to fetch data for.</param>
    /// <returns>Collection of coin asset information including price, market cap, and stablecoin status.</returns>
    /// <response code="200">Returns the collection of coin asset information.</response>
    /// <response code="400">If no CoinGecko IDs are provided.</response>
    /// <response code="500">If something went wrong during assets info retrievement.</response>
    [HttpGet("coingecko/assets-info")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IEnumerable<CoinGeckoAssetInfo>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetCoinGeckoAssetsInfo(
        [FromQuery] IEnumerable<string> coinGeckoIds
    )
    {
        var response = coinGeckoIds.Any()
            ? await _coinGeckoService.GetCoinGeckoAssetsInfo(coinGeckoIds)
            : Result.Fail(new GenericErrors.BadRequestError("CoinGecko IDs must be provided."));
        return response.ToActionResult(this);
    }
}
