using Microsoft.AspNetCore.Mvc;
using SVC_Coins.Models.Input;
using SVC_Coins.Models.Output;
using SVC_Coins.Repositories.Interfaces;

namespace SVC_Coins.Controllers;

/// <summary>
/// Controller for handling coins operations.
/// </summary>
[ApiController]
[Route("coins")]
public class CoinsController(ICoinsRepository repository) : ControllerBase
{
    private readonly ICoinsRepository _repository = repository;

    /// <summary>
    /// Inserts new coin into the database.
    /// </summary>
    /// <param name="coin">The coin object to insert.</param>
    /// <returns>A status indicating the result of the operation.</returns>
    /// <response code="204">The coin was successfully inserted.</response>
    /// <response code="409">The coin already exists in the database.</response>
    [HttpPost("insert")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(string), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> InsertCoin([FromBody] CoinNew coin)
    {
        var result = await _repository.InsertCoin(coin);
        return result.IsSuccess ? NoContent() : Conflict(result.Errors.First().Message);
    }

    /// <summary>
    /// Retrieves all coins from the database.
    /// </summary>
    /// <returns>A list of all coins entries.</returns>
    /// <response code="200">The list of all coins was successfuly retrieved.</response>
    [HttpGet("all")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IEnumerable<Coin>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllCoins()
    {
        var coinsList = await _repository.GetAllCoins();
        return Ok(coinsList);
    }

    /// <summary>
    /// Deletes a coin from the database.
    /// </summary>
    /// <param name="idCoin">The ID of the coin.</param>
    /// <returns>A status indicating the result of the operation.</returns>
    /// <response code="204">The coin was successfully deleted.</response>
    /// <response code="404">The coin was not found in the database.</response>
    [HttpDelete("{idCoin}")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCoin([FromRoute] int idCoin)
    {
        var result = await _repository.DeleteCoin(idCoin);
        return result.IsSuccess ? NoContent() : NotFound(result.Errors.First().Message);
    }

    /// <summary>
    /// Inserts new trading pair into the database.
    /// </summary>
    /// <param name="tradingPair">The trading pair object to insert.</param>
    /// <returns>The ID of the inserted trading pair.</returns>
    /// <response code="200">The trading pair was successfully inserted.</response>
    /// <response code="400">The trading pair already exists or one or both coins do not exist.</response>
    [HttpPost("tradingPairs/insert")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> InsertTradingPair([FromBody] TradingPairNew tradingPair)
    {
        var result = await _repository.InsertTradingPair(tradingPair);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors.First().Message);
    }

    /// <summary>
    /// Retrieves quote coins sorted by priority.
    /// </summary>
    /// <returns>A list of quote coins sorted by priority.</returns>
    /// <response code="200">The list of quote coins sorted by priority was successfuly retrieved.</response>
    [HttpGet("quoteCoinsPrioritized")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IEnumerable<Coin>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetQuoteCoinsPrioritized()
    {
        var prioritizedQuoteCoins = await _repository.GetQuoteCoinsPrioritized();
        return Ok(prioritizedQuoteCoins);
    }
}
