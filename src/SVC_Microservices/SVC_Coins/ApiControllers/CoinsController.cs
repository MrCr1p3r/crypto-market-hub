using Microsoft.AspNetCore.Mvc;
using SharedLibrary.Extensions;
using SVC_Coins.ApiContracts.Requests;
using SVC_Coins.ApiContracts.Requests.CoinCreation;
using SVC_Coins.ApiContracts.Responses;
using SVC_Coins.Services;

namespace SVC_Coins.ApiControllers;

/// <summary>
/// Controller for handling coins operations.
/// </summary>
[ApiController]
[Route("coins")]
public class CoinsController(ICoinsService coinsService) : ControllerBase
{
    private readonly ICoinsService _coinsService = coinsService;

    /// <summary>
    /// Retrieves coins from the system. If no IDs are provided, all coins are retrieved.
    /// </summary>
    /// <param name="ids">The IDs of the coins to retrieve (optional).</param>
    /// <returns>A list of retrieved coins.</returns>
    /// <response code="200">The coins were successfully retrieved.</response>
    /// <response code="500">Internal error occurred during coins retrieval operation.</response>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IEnumerable<Coin>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetCoins([FromQuery] IEnumerable<int> ids)
    {
        if (!ids.Any())
        {
            var coinsList = await _coinsService.GetAllCoins();
            return Ok(coinsList);
        }

        var coins = await _coinsService.GetCoinsByIds(ids);
        return Ok(coins);
    }

    /// <summary>
    /// Creates multiple new coins along with their trading pairs.
    /// </summary>
    /// <param name="coins">The collection of creation requests.</param>
    /// <returns>A collection of created coins.</returns>
    /// <response code="200">The coins and trading pairs were successfully created.</response>
    /// <response code="400">One or more validation errors occurred.</response>
    /// <response code="500">Internal error occurred during coins creating operation.</response>
    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IEnumerable<Coin>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateCoins([FromBody] IEnumerable<CoinCreationRequest> coins)
    {
        var result = await _coinsService.CreateCoinsWithTradingPairs(coins);
        return result.ToActionResult(this);
    }

    /// <summary>
    /// Creates multiple new quote coins.
    /// </summary>
    /// <param name="quoteCoins">The collection of quote coin creation requests.</param>
    /// <returns>A collection of created quote coins.</returns>
    /// <response code="200">The quote coins were successfully created.</response>
    /// <response code="400">One or more validation errors occurred.</response>
    /// <response code="500">Internal error occurred during quote coins creating operation.</response>
    [HttpPost("quote")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IEnumerable<TradingPairCoinQuote>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateQuoteCoins(
        [FromBody] IEnumerable<QuoteCoinCreationRequest> quoteCoins
    )
    {
        var result = await _coinsService.CreateQuoteCoins(quoteCoins);
        return result.ToActionResult(this);
    }

    /// <summary>
    /// Updates the market data for multiple coins.
    /// </summary>
    /// <param name="requests">Collection of coin market data update requests.</param>
    /// <returns>A list of updated coins.</returns>
    /// <response code="200">Market data successfully updated.</response>
    /// <response code="404">One or more provided coins do not exist in the system.</response>
    /// <response code="500">Internal error occurred during market data update operation.</response>
    [HttpPatch("market-data")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IEnumerable<Coin>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateMarketData(
        [FromBody] IEnumerable<CoinMarketDataUpdateRequest> requests
    )
    {
        var result = await _coinsService.UpdateCoinsMarketData(requests);
        return result.ToActionResult(this);
    }

    /// <summary>
    /// Replaces all existing trading pairs with the provided ones.
    /// </summary>
    /// <param name="requests">The collection of trading pair creation requests.</param>
    /// <returns>A list of the coins with the new trading pairs.</returns>
    /// <response code="200">Trading pairs were successfully replaced.</response>
    /// <response code="400">One or more validation errors occurred.</response>
    /// <response code="500">Internal error occurred during trading pairs replacing operation.</response>
    [HttpPut("trading-pairs")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IEnumerable<Coin>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ReplaceTradingPairs(
        [FromBody] IEnumerable<TradingPairCreationRequest> requests
    )
    {
        var result = await _coinsService.ReplaceAllTradingPairs(requests);
        return result.ToActionResult(this);
    }

    /// <summary>
    /// Deletes a coin from the system.
    /// </summary>
    /// <param name="idCoin">The ID of the coin that should be deleted.</param>
    /// <returns>A status indicating the result of the operation.</returns>
    /// <response code="204">The coin was successfully deleted.</response>
    /// <response code="404">The coin was not found in the database.</response>
    /// <response code="500">Internal error occurred during coin deletion operation.</response>
    [HttpDelete("{idCoin}")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteCoin([FromRoute] int idCoin)
    {
        var result = await _coinsService.DeleteCoinWithTradingPairs(idCoin);
        return result.ToActionResult(this);
    }

    /// <summary>
    /// Deletes all coins that are neither referenced as a base nor a quote coin in any trading pair.
    /// </summary>
    /// <returns>A status indicating the result of the operation.</returns>
    /// <response code="204">All unreferenced coins were successfully deleted.</response>
    /// <response code="500">Internal error occurred during orphaned‐coins deletion operation.</response>
    [HttpDelete("unreferenced")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteUnreferencedCoins()
    {
        await _coinsService.DeleteCoinsNotReferencedByTradingPairs();
        return NoContent();
    }

    /// <summary>
    /// Deletes all coins (and, via cascade, any related data) from the system.
    /// </summary>
    /// <returns>A status indicating the result of the operation.</returns>
    /// <response code="204">All data was successfully deleted.</response>
    /// <response code="500">Internal error occurred during data deletion operation.</response>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteAllCoins()
    {
        await _coinsService.DeleteAllCoinsWithRelatedData();
        return NoContent();
    }
}
