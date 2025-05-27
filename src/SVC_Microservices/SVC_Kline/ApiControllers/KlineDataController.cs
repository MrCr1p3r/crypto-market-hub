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
    /// <returns>A dictionary where key is the trading pair ID and value is a collection of
    /// Kline data entries for that trading pair.</returns>
    /// <response code="200">Returns the Kline data grouped by trading pair ID.</response>
    /// <response code="500">Something went wrong during kline data retrieval.</response>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(
        typeof(IReadOnlyDictionary<int, IEnumerable<KlineData>>),
        StatusCodes.Status200OK
    )]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllKlineData()
    {
        var klineDataDict = await _repository.GetAllKlineData();
        return Ok(klineDataDict);
    }

    /// <summary>
    /// Inserts multiple Kline data entries into the database.
    /// </summary>
    /// <param name="klineDataList">A list of KlineData objects to insert.</param>
    /// <returns>A status indicating the result of the operation.</returns>
    /// <response code="200">Returns a status indicating the result of the operation.</response>
    /// <response code="500">Something went wrong during kline data insertion.</response>
    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> InsertKlineData(
        [FromBody] IEnumerable<KlineDataCreationRequest> klineDataList
    )
    {
        await _repository.InsertKlineData(klineDataList);
        return Ok("Multiple Kline data entries inserted successfully.");
    }

    /// <summary>
    /// Replaces all Kline data with new entries.
    /// </summary>
    /// <param name="klineDataList">An array of KlineData objects to replace existing data.</param>
    /// <returns>A status indicating the result of the operation.</returns>
    /// <response code="200">Returns a status indicating the result of the operation.</response>
    /// <response code="500">Something went wrong during kline data replacement.</response>
    [HttpPut]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ReplaceAllKlineData(
        [FromBody] KlineDataCreationRequest[] klineDataList
    )
    {
        await _repository.ReplaceAllKlineData(klineDataList);
        return Ok("All Kline data replaced successfully.");
    }
}
