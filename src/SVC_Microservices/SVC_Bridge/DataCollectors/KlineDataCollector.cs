using SharedLibrary.Enums;
using SVC_Bridge.Clients.Interfaces;
using SVC_Bridge.DataCollectors.Interfaces;
using SVC_Bridge.DataDistributors.Interfaces;
using SVC_Bridge.Models.Input;
using SVC_Bridge.Models.Output;

namespace SVC_Bridge.DataCollectors;

public class KlineDataCollector(
    ISvcCoinsClient coinsClient,
    ISvcExternalClient externalClient,
    IKlineDataDistributor klineDataDistributor,
    ILogger<KlineDataCollector> logger
) : IKlineDataCollector
{
    private readonly ISvcCoinsClient _coinsClient = coinsClient;
    private readonly ISvcExternalClient _externalClient = externalClient;
    private readonly IKlineDataDistributor _klineDataDistributor = klineDataDistributor;
    private readonly ILogger<KlineDataCollector> _logger = logger;

    /// <inheritdoc />
    public async Task<IEnumerable<KlineDataNew>> CollectEntireKlineData()
    {
        var allCoins = await _coinsClient.GetAllCoins();
        return await CollectAllKlineData(allCoins);
    }

    private async Task<IEnumerable<KlineDataNew>> CollectAllKlineData(IEnumerable<Coin> allCoins)
    {
        var quoteCoinsPrioritization = await _coinsClient.GetQuoteCoinsPrioritized();
        var klineDataCollectionTasks = allCoins.Select(coin =>
            CollectKlineDataForCoin(coin, quoteCoinsPrioritization)
        );
        var allKlineData = await Task.WhenAll(klineDataCollectionTasks);
        return allKlineData.SelectMany(result => result);
    }

    private async Task<IEnumerable<KlineDataNew>> CollectKlineDataForCoin(
        Coin mainCoin,
        IEnumerable<Coin> quoteCoinsPrioritization
    )
    {
        foreach (var quoteCoin in quoteCoinsPrioritization)
        {
            var klineData = await GetKlineData(mainCoin.Symbol, quoteCoin.Symbol);
            if (!klineData.Any())
                continue;

            var idTradingPair = GetTradingPairId(mainCoin.TradingPairs, quoteCoin);
            idTradingPair ??= await _klineDataDistributor.InsertTradingPair(
                mainCoin.Id,
                quoteCoin.Id
            );

            return klineData.Select(kd => Mapping.ToKlineDataNew(kd, idTradingPair.Value));
        }

        _logger.LogWarning(
            "No kline data could be fetched for {symbol} - {name}.",
            mainCoin.Symbol,
            mainCoin.Name
        );
        return [];
    }

    private async Task<IEnumerable<KlineData>> GetKlineData(
        string symbolCoinMain,
        string symbolCoinQuote
    )
    {
        var klineDataRequest = Mapping.ToKlineDataRequest(symbolCoinMain, symbolCoinQuote);
        return await _externalClient.GetKlineData(klineDataRequest);
    }

    private static int? GetTradingPairId(
        IEnumerable<TradingPair> existingTradingPairs,
        Coin quoteCoin
    ) => existingTradingPairs.FirstOrDefault(tp => AreCoinsEquivalent(tp.CoinQuote, quoteCoin))?.Id;

    private static bool AreCoinsEquivalent(TradingPairCoin coin, Coin quoteCoin) =>
        coin.Symbol.Equals(quoteCoin.Symbol, StringComparison.OrdinalIgnoreCase)
        && coin.Name.Equals(quoteCoin.Name, StringComparison.OrdinalIgnoreCase);

    private static class Mapping
    {
        public static KlineDataNew ToKlineDataNew(KlineData request, int idTradePair) =>
            new()
            {
                IdTradePair = idTradePair,
                OpenTime = request.OpenTime,
                OpenPrice = request.OpenPrice,
                HighPrice = request.HighPrice,
                LowPrice = request.LowPrice,
                ClosePrice = request.ClosePrice,
                Volume = request.Volume,
                CloseTime = request.CloseTime,
            };

        public static KlineDataRequest ToKlineDataRequest(string coinMain, string coinQuote) =>
            new()
            {
                CoinMainSymbol = coinMain,
                CoinQuoteSymbol = coinQuote,
                Interval = ExchangeKlineInterval.FourHours,
                StartTime = DateTime.UtcNow.AddDays(-7),
                EndTime = DateTime.UtcNow,
            };
    }
}
