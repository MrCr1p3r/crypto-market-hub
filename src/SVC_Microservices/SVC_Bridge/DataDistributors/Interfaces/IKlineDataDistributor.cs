namespace SVC_Bridge.DataDistributors.Interfaces;

/// <summary>
/// Interface for handling trading pair data distribution.
/// </summary>
public interface IKlineDataDistributor
{
    /// <summary>
    /// Inserts a new trading pair into the database.
    /// </summary>
    /// <param name="idCoinMain">The ID of the main coin.</param>
    /// <param name="idCoinQuote">The ID of the quote coin.</param>
    /// <returns>The ID of the created trading pair.</returns>
    Task<int> InsertTradingPair(int idCoinMain, int idCoinQuote);
}
