using Microsoft.AspNetCore.Mvc;
using SVC_Coins.Models.Input;
using SVC_Coins.Models.Output;
using SVC_Coins.Repositories.Interfaces;

namespace SVC_Coins.Controllers;

/// <summary>
/// Controller for handling coins operations.
/// </summary>
/// <param name="repository">The coins repository.</param>
[ApiController]
[Route("api/[controller]")]
public class CoinsController(ICoinsRepository repository) : ControllerBase
{
    private readonly ICoinsRepository _repository = repository;

    /// <summary>
    /// Inserts new coin into the database.
    /// </summary>
    /// <param name="coin">The coin object to insert.</param>
    /// <returns>A status indicating the result of the operation.</returns>
    [HttpPost("insert")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> InsertCoin([FromBody] CoinNew coin)
    {
        await _repository.InsertCoin(coin);
        return Ok("Coin inserted successfully.");
    }

    /// <summary>
    /// Retrieves all coins from the database.
    /// </summary>
    /// <returns>A list of all coins entries.</returns>
    [HttpGet("getAll")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IEnumerable<Coin>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Coin>>> GetAllCoins()
    {
        var coinsList = await _repository.GetAllCoins();
        return Ok(coinsList);
    }

    /// <summary>
    /// Deletes a coin from the database.
    /// </summary>
    /// <param name="idCoin">The ID of the coin.</param>
    /// <returns>A status indicating the result of the operation.</returns>
    [HttpDelete("delete/{idCoin}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteCoin([FromRoute] int idCoin)
    {
        await _repository.DeleteCoin(idCoin);
        return Ok($"Coin with ID {idCoin} deleted successfully.");
    }
    
    /// <summary>
    /// Inserts new trading pair into the database.
    /// </summary>
    /// <param name="tradingPair">The trading pair object to insert.</param>
    /// <returns>The ID of the inserted trading pair.</returns>
    [HttpPost("tradingPair/insert")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<IActionResult> InsertTradingPair([FromBody] TradingPairNew tradingPair)
    {
        var insertedId = await _repository.InsertTradingPair(tradingPair);
        return Ok(insertedId);
    }
}
