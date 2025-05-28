using Microsoft.AspNetCore.Mvc;
using SharedLibrary.Extensions;
using SVC_External.ApiContracts.Requests;
using SVC_External.ApiContracts.Responses.Exchanges.KlineData;
using SVC_External.Services.Exchanges.Interfaces;

namespace SVC_External.ApiControllers.Exchanges;

/// <summary>
/// Controller for handling exchanges kline data operations.
/// </summary>
[ApiController]
[Route("exchanges/kline")]
public class KlineDataController(IKlineDataService klineDataService) : ControllerBase
{
    private readonly IKlineDataService _klineDataService = klineDataService;

    /// <summary>
    /// Fetches Kline (candlestick) data for a specific trading pair from available exchanges.
    /// </summary>
    /// <param name="request">The request parameters for fetching Kline data.</param>
    /// <returns>A Kline data response containing trading pair ID and kline data.</returns>
    /// <response code="200">Returns the Kline data response.</response>
    /// <response code="500">If something went wrong during Kline data retrieval.</response>
    [HttpPost("query")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(KlineDataResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetKlineDataForTradingPair([FromBody] KlineDataRequest request)
    {
        var klineData = await _klineDataService.GetKlineDataForTradingPair(request);
        return klineData.ToActionResult(this);
    }

    /// <summary>
    /// Fetches Kline (candlestick) data for multiple coins and trading pairs from available exchanges.
    /// </summary>
    /// <param name="request">The batch request parameters for fetching Kline data.</param>
    /// <returns>A dictionary where the key is the trading pair ID and the value is the collection of kline data for that trading pair.</returns>
    /// <response code="200">Returns a dictionary of trading pair IDs to kline data collections.</response>
    /// <response code="500">If something went wrong during Kline data retrieval.</response>
    [HttpPost("query/bulk")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IEnumerable<KlineDataResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetFirstSuccessfulKlineDataPerCoin(
        [FromBody] KlineDataBatchRequest request
    )
    {
        var response = await _klineDataService.GetFirstSuccessfulKlineDataPerCoin(request);
        return Ok(response);
    }
}
