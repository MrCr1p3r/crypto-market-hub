using Microsoft.AspNetCore.Mvc;
using SVC_Bridge.Clients.Interfaces;
using SVC_Bridge.DataCollectors.Interfaces;
using SVC_Bridge.Models.Input;

namespace SVC_Bridge.Controllers;

/// <summary>
/// Controller for handling Kline data operations.
/// </summary>
[ApiController]
[Route("bridge/kline")]
public class KlineDataController(
    IKlineDataCollector klineDataCollector,
    ISvcKlineClient klineClient
) : ControllerBase
{
    private readonly IKlineDataCollector _klineDataCollector = klineDataCollector;
    private readonly ISvcKlineClient _klineClient = klineClient;

    /// <summary>
    /// Updates the entire Kline data for all coins.
    /// </summary>
    /// <param name="request">The kline data request parameters.</param>
    /// <returns>A status indicating the result of the operation.</returns>
    [HttpPost("updateEntireKlineData")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateEntireKlineData([FromBody] KlineDataRequest request)
    {
        var klineData = await _klineDataCollector.CollectEntireKlineData(request);
        if (!klineData.Any())
            return BadRequest("No kline data was collected.");

        await _klineClient.ReplaceAllKlineData(klineData);
        return Ok("Kline data updated successfully.");
    }
}
