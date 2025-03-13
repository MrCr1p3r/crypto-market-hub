using FluentResults;
using Microsoft.AspNetCore.Mvc;
using SharedLibrary.Errors;
using SharedLibrary.Extensions;
using SVC_External.DataCollectors.Interfaces;
using SVC_External.Models.Input;
using SVC_External.Models.Output;

namespace SVC_External.Controllers;

/// <summary>
/// Controller for handling exchanges operations.
/// </summary>
[ApiController]
[Route("exchanges")]
public class ExchangesController(IExchangesDataCollector dataCollector) : ControllerBase
{
    private readonly IExchangesDataCollector _dataCollector = dataCollector;

    /// <summary>
    /// Retrieves all spot coins from all available exchanges.
    /// </summary>
    /// <returns>Collection of coins, listen on all of the available exchanges.</returns>
    /// <response code="200">Returns the list of coins.</response>
    /// <response code="500">If something went wrong during coins retrieval.</response>
    [HttpGet("spot/coins")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IEnumerable<Coin>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllSpotCoins()
    {
        var coins = await _dataCollector.GetAllCurrentActiveSpotCoins();
        return coins.ToActionResult(this);
    }

    /// <summary>
    /// Fetches Kline (candlestick) data for a specific trading pair from available exchanges.
    /// </summary>
    /// <param name="request">The request parameters for fetching Kline data.</param>
    /// <returns>A Kline data response containing trading pair ID and kline data.</returns>
    /// <response code="200">Returns the Kline data response.</response>
    /// <response code="500">If something went wrong during Kline data retrieval.</response>
    [HttpPost("klineData/query")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(KlineDataRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetKlineDataForTradingPair([FromBody] KlineDataRequest request)
    {
        var klineData = await _dataCollector.GetKlineDataForTradingPair(request);
        return klineData.ToActionResult(this);
    }

    /// <summary>
    /// Fetches Kline (candlestick) data for multiple coins and trading pairs from available exchanges.
    /// </summary>
    /// <param name="request">The batch request parameters for fetching Kline data.</param>
    /// <returns>A collection of Kline data responses, each containing trading pair ID and kline data.</returns>
    /// <response code="200">Returns the collection of Kline data responses.</response>
    /// <response code="500">If something went wrong during Kline data retrieval.</response>
    [HttpPost("klineData/batchQuery")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IEnumerable<KlineDataRequestResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetFirstSuccessfulKlineDataPerCoin(
        [FromBody] KlineDataBatchRequest request
    )
    {
        var response = await _dataCollector.GetFirstSuccessfulKlineDataPerCoin(request);
        return Ok(response);
    }

    /// <summary>
    /// Get market data and stablecoin status for specified coin IDs from CoinGecko.
    /// </summary>
    /// <param name="coinGeckoIds">Array of CoinGecko coin IDs to fetch data for.</param>
    /// <returns>Collection of coin asset information including price, market cap, and stablecoin status.</returns>
    /// <response code="200">Returns the collection of coin asset information.</response>
    /// <response code="400">If no CoinGecko IDs are provided.</response>
    /// <response code="500">If something went wrong during assets info retrievement.</response>
    [HttpGet("coins/marketInfo")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IEnumerable<CoinGeckoAssetInfo>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetCoinGeckoAssetsInfo(
        [FromQuery] IEnumerable<string> coinGeckoIds
    )
    {
        var response = coinGeckoIds.Any()
            ? await _dataCollector.GetCoinGeckoAssetsInfo(coinGeckoIds)
            : Result.Fail(new GenericErrors.BadRequestError("CoinGecko IDs must be provided."));
        return response.ToActionResult(this);
    }
}
