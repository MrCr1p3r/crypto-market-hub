namespace SVC_Coins.ApiContracts.Requests.CoinCreation;

/// <summary>
/// Contains data for trading pair's quote coin.
/// </summary>
public record CoinCreationCoinQuote : CoinCreationCoinBase
{
    /// <summary>
    /// Id of the quote coin coin.
    /// </summary>
    /// <remarks>
    /// If not provided, this quote coin will be added to the database.
    /// </remarks>
    public int? Id { get; set; }
}
