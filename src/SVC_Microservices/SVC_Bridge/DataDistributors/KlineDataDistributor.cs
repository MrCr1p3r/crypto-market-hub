using SVC_Bridge.Clients.Interfaces;
using SVC_Bridge.DataDistributors.Interfaces;
using SVC_Bridge.Models.Input;
using SVC_Bridge.Models.Output;

namespace SVC_Bridge.DataDistributors;

/// <summary>
/// Handles distribution and insertion of trading pair data.
/// </summary>
public class KlineDataDistributor(ISvcCoinsClient coinsClient) : IKlineDataDistributor
{
    private readonly ISvcCoinsClient _coinsClient = coinsClient;

    /// <inheritdoc />
    public async Task<int> InsertTradingPair(int idCoinMain, int idCoinQuote)
    {
        var tradingPair = Mapping.ToTradingPairNew(idCoinMain, idCoinQuote);
        return await _coinsClient.InsertTradingPair(tradingPair);
    }

    private static class Mapping
    {
        public static TradingPairNew ToTradingPairNew(int idCoinMain, int idCoinQuote) =>
            new() { IdCoinMain = idCoinMain, IdCoinQuote = idCoinQuote };
    }
}
