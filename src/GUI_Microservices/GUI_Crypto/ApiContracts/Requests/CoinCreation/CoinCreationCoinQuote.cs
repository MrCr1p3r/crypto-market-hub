namespace GUI_Crypto.ApiContracts.Requests.CoinCreation;

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
    /// If provided, it will be used for trading pair creation.
    /// </remarks>
    public int? Id { get; set; }
}
