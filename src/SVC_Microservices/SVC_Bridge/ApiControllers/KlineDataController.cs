using Microsoft.AspNetCore.Mvc;
using SharedLibrary.Extensions;
using SVC_Bridge.ApiContracts.Responses.KlineData;
using SVC_Bridge.Services.Interfaces;

namespace SVC_Bridge.ApiControllers;

/// <summary>
/// Controller for handling kline data operations.
/// </summary>
[ApiController]
[Route("bridge/kline")]
public class KlineDataController(IKlineDataService klineDataService) : ControllerBase
{
    private readonly IKlineDataService _klineDataService = klineDataService;

    /// <summary>
    /// Updates the kline data for all coins in the system.
    /// </summary>
    /// <returns>A collection of updated kline data grouped by trading pairs.</returns>
    /// <response code="200">Kline data successfully updated for all trading pairs.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="500">Internal error occurred during kline data update operation.</response>
    [HttpPost]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IEnumerable<KlineDataResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateKlineData()
    {
        var result = await _klineDataService.UpdateKlineData();
        return result.ToActionResult(this);
    }
}
