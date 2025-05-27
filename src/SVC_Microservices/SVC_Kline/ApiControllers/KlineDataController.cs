using Microsoft.AspNetCore.Mvc;
using SVC_Kline.ApiContracts.Requests;
using SVC_Kline.ApiContracts.Responses;
using SVC_Kline.Repositories;

namespace SVC_Kline.ApiControllers;

/// <summary>
/// Controller for handling Kline data operations.
/// </summary>
/// <param name="repository">The Kline data repository.</param>
[ApiController]
[Route("kline")]
public class KlineDataController(IKlineDataRepository repository) : ControllerBase
{
    private readonly IKlineDataRepository _repository = repository;

    /// <summary>
    /// Retrieves all Kline data from the database grouped by trading pair ID.
    /// </summary>
    /// <returns>A collection of KlineDataResponse objects, each containing data for one trading pair.</returns>
    /// <response code="200">Returns the Kline data grouped by trading pair ID.</response>
    /// <response code="500">Something went wrong during kline data retrieval.</response>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IEnumerable<KlineDataResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllKlineData()
    {
        var klineDataResponses = await _repository.GetAllKlineData();
        return Ok(klineDataResponses);
    }

    /// <summary>
    /// Inserts multiple Kline data entries into the database.
    /// </summary>
    /// <param name="klineDataList">A list of KlineDataCreationRequest objects to insert.</param>
    /// <returns>A collection of KlineDataResponse objects containing the freshly inserted data grouped by trading pair.</returns>
    /// <response code="200">Returns the freshly inserted Kline data grouped by trading pair ID.</response>
    /// <response code="500">Something went wrong during kline data insertion.</response>
    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IEnumerable<KlineDataResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> InsertKlineData(
        [FromBody] IEnumerable<KlineDataCreationRequest> klineDataList
    )
    {
        var insertedData = await _repository.InsertKlineData(klineDataList);
        return Ok(insertedData);
    }

    /// <summary>
    /// Replaces all Kline data with new entries.
    /// </summary>
    /// <param name="klineDataList">An array of KlineDataCreationRequest objects to replace existing data.</param>
    /// <returns>A collection of KlineDataResponse objects containing the freshly inserted data grouped by trading pair.</returns>
    /// <response code="200">Returns the freshly inserted Kline data grouped by trading pair ID.</response>
    /// <response code="500">Something went wrong during kline data replacement.</response>
    [HttpPut]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IEnumerable<KlineDataResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ReplaceAllKlineData(
        [FromBody] KlineDataCreationRequest[] klineDataList
    )
    {
        var replacedData = await _repository.ReplaceAllKlineData(klineDataList);
        return Ok(replacedData);
    }
}
