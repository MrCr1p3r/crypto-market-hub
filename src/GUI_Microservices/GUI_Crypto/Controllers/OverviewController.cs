using GUI_Crypto.Clients.Interfaces;
using GUI_Crypto.Models.Input;
using GUI_Crypto.ViewModels.Factories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using SVC_External.Models.Output;

namespace GUI_Crypto.Controllers;

/// <summary>
/// Controller for handling the overview page related operations.
/// </summary>
public class OverviewController(
    ICryptoViewModelFactory viewModelFactory,
    ISvcExternalClient externalClient,
    ISvcCoinsClient coinsClient
) : Controller
{
    private readonly ICryptoViewModelFactory _viewModelFactory = viewModelFactory;
    private readonly ISvcExternalClient _externalClient = externalClient;
    private readonly ISvcCoinsClient _coinsClient = coinsClient;

    /// <summary>
    /// Renders the Overview view.
    /// </summary>
    /// <returns>Rendered view.</returns>
    /// <response code="200">View rendered successfully.</response>
    [HttpGet("overview")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> RenderOverview()
    {
        var viewModel = await _viewModelFactory.CreateOverviewViewModel();
        return View("Overview", viewModel);
    }

    /// <summary>
    /// Gets all listed coins from external service.
    /// </summary>
    /// <returns>List of coins listed on each exchange.</returns>
    /// <response code="200">List of coins retrieved successfully.</response>
    [HttpGet("listed-coins")]
    [ProducesResponseType(typeof(ListedCoins), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetListedCoins()
    {
        var coins = await _externalClient.GetAllListedCoins();
        return Ok(coins);
    }

    /// <summary>
    /// Creates a new coin.
    /// </summary>
    /// <param name="coin">The data of the coin to create.</param>
    /// <returns>Success status.</returns>
    /// <response code="200">Coin created successfully.</response>
    /// <response code="409">Coin already exists in the database.</response>
    [HttpPost("coin")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateCoin([FromBody] CoinNew coin)
    {
        var result = await _coinsClient.CreateCoin(coin);
        return result ? Ok() : Conflict();
    }

    /// <summary>
    /// Deletes a coin by its ID.
    /// </summary>
    /// <param name="idCoin">The ID of the coin to delete.</param>
    /// <returns>Success status.</returns>
    /// <response code="200">Coin deleted successfully.</response>
    [HttpDelete("coin/{idCoin}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteCoin([FromRoute] int idCoin)
    {
        await _coinsClient.DeleteCoin(idCoin);
        return Ok();
    }
}
