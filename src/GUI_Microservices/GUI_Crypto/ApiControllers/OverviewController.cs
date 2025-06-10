using GUI_Crypto.ApiContracts.Requests.CoinCreation;
using GUI_Crypto.ApiContracts.Responses.CandidateCoin;
using GUI_Crypto.ApiContracts.Responses.OverviewCoin;
using GUI_Crypto.Services.Overview;
using Microsoft.AspNetCore.Mvc;
using SharedLibrary.Extensions;

namespace GUI_Crypto.ApiControllers;

/// <summary>
/// Controller for handling the overview page related operations.
/// </summary>
public class OverviewController(IOverviewService overviewService) : Controller
{
    private readonly IOverviewService _overviewService = overviewService;

    /// <summary>
    /// Renders the Overview view.
    /// </summary>
    /// <returns>Rendered view.</returns>
    /// <response code="200">View rendered successfully.</response>
    [HttpGet("overview")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public IActionResult RenderOverview()
    {
        return View("Overview");
    }

    /// <summary>
    /// Retrieves all coins from the system for the overview page..
    /// </summary>
    /// <returns>Collection of all overview coins with associated kline data.</returns>
    /// <response code="200">Coins retrieved successfully.</response>
    /// <response code="500">Internal error occurred during coins retrieval operation.</response>
    [HttpGet("coins")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IEnumerable<OverviewCoin>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetOverviewCoins()
    {
        var coinsResult = await _overviewService.GetOverviewCoins();
        return coinsResult.ToActionResult(this);
    }

    /// <summary>
    /// Retrieves all candidate coins that could be added to the system.
    /// </summary>
    /// <returns>Collection of candidate coins available for insertion.</returns>
    /// <response code="200">Candidate coins retrieved successfully.</response>
    /// <response code="500">Internal error occurred during candidate coins retrieval operation.</response>
    [HttpGet("candidate-coins")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IEnumerable<CandidateCoin>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetCandidateCoins()
    {
        var candidateCoinsResult = await _overviewService.GetCandidateCoins();
        return candidateCoinsResult.ToActionResult(this);
    }

    /// <summary>
    /// Creates multiple coins in the system.
    /// </summary>
    /// <param name="coins">Collection of coin creation requests.</param>
    /// <returns>Collection of created overview coins with associated data.</returns>
    /// <response code="200">Coins created successfully.</response>
    /// <response code="400">Invalid coin data provided.</response>
    /// <response code="500">Internal error occurred during coin creation operation.</response>
    [HttpPost("coins")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IEnumerable<OverviewCoin>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateCoins([FromBody] IEnumerable<CoinCreationRequest> coins)
    {
        var createResult = await _overviewService.CreateCoins(coins);
        return createResult.ToActionResult(this);
    }

    /// <summary>
    /// Deletes a specific coin from the system.
    /// </summary>
    /// <param name="idCoin">The ID of the coin to delete.</param>
    /// <returns>Result of the deletion operation.</returns>
    /// <response code="200">Coin deleted successfully.</response>
    /// <response code="404">Coin with the specified ID was not found.</response>
    /// <response code="500">Internal error occurred during coin deletion operation.</response>
    [HttpDelete("coins/{idCoin:int}")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteMainCoin(int idCoin)
    {
        var deleteResult = await _overviewService.DeleteMainCoin(idCoin);
        return deleteResult.ToActionResult(this);
    }

    /// <summary>
    /// Deletes all coins and related data from the system.
    /// </summary>
    /// <returns>Result of the deletion operation.</returns>
    /// <response code="200">All coins deleted successfully.</response>
    /// <response code="500">Internal error occurred during coins deletion operation.</response>
    [HttpDelete("coins")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteAllCoins()
    {
        var deleteResult = await _overviewService.DeleteAllCoins();
        return deleteResult.ToActionResult(this);
    }
}
