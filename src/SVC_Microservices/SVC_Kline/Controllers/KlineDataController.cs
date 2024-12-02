using Microsoft.AspNetCore.Mvc;
using SVC_Kline.Models.Input;
using SVC_Kline.Models.Output;
using SVC_Kline.Repositories.Interfaces;

namespace SVC_Kline.Controllers;

/// <summary>
/// Controller for handling Kline data operations.
/// </summary>
/// <param name="repository">The Kline data repository.</param>
[ApiController]
[Route("api/[controller]")]
public class KlineDataController(IKlineDataRepository repository) : ControllerBase
{
    private readonly IKlineDataRepository _repository = repository;

    /// <summary>
    /// Inserts new Kline data into the database.
    /// </summary>
    /// <param name="klineData">The KlineData object to insert.</param>
    /// <returns>A status indicating the result of the operation.</returns>
    [HttpPost("insert")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> InsertKlineData([FromBody] KlineDataNew klineData)
    {
        await _repository.InsertKlineData(klineData);
        return Ok("Kline data inserted successfully.");
    }

    /// <summary>
    /// Inserts multiple Kline data entries into the database.
    /// </summary>
    /// <param name="klineDataList">A list of KlineData objects to insert.</param>
    /// <returns>A status indicating the result of the operation.</returns>
    [HttpPost("insertMany")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> InsertManyKlineData([FromBody] IEnumerable<KlineDataNew> klineDataList)
    {
        await _repository.InsertManyKlineData(klineDataList);
        return Ok("Multiple Kline data entries inserted successfully.");
    }

    /// <summary>
    /// Retrieves all Kline data from the database.
    /// </summary>
    /// <returns>A list of all Kline data entries.</returns>
    [HttpGet("getAll")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IEnumerable<KlineData>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<KlineData>>> GetAllKlineData()
    {
        var klineDataList = await _repository.GetAllKlineData();
        return Ok(klineDataList);
    }

    /// <summary>
    /// Deletes all Kline data for a specific trading pair.
    /// </summary>
    /// <param name="idTradePair">The ID of the trading pair.</param>
    /// <returns>A status indicating the result of the operation.</returns>
    [HttpDelete("delete/{idTradePair}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteKlineDataForTradingPair([FromRoute] int idTradePair)
    {
        await _repository.DeleteKlineDataForTradingPair(idTradePair);
        return Ok($"Kline data for trading pair ID {idTradePair} deleted successfully.");
    }

    /// <summary>
    /// Replaces all Kline data with new entries.
    /// </summary>
    /// <param name="klineDataList">An array of KlineData objects to replace existing data.</param>
    /// <returns>A status indicating the result of the operation.</returns>
    [HttpPut("replaceAll")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> ReplaceAllKlineData([FromBody] KlineDataNew[] klineDataList)
    {
        await _repository.ReplaceAllKlineData(klineDataList);
        return Ok("All Kline data replaced successfully.");
    }
}
