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
    public async Task<IEnumerable<KlineDataNew>> CollectEntireKlineData(KlineDataRequest request)
    {
        var allCoins = await _coinsClient.GetAllCoins();
        var coinsWithoutStablecoins = allCoins.Where(coin => !coin.IsStablecoin);
        return await CollectAllKlineData(coinsWithoutStablecoins, request);
    }

    private async Task<IEnumerable<KlineDataNew>> CollectAllKlineData(
        IEnumerable<Coin> coins,
        KlineDataRequest request
    )
    {
        var quoteCoinsPrioritization = await _coinsClient.GetQuoteCoinsPrioritized();
        var klineDataCollectionTasks = coins.Select(coin =>
            CollectKlineDataForCoin(coin, quoteCoinsPrioritization, request)
        );
        var allKlineData = await Task.WhenAll(klineDataCollectionTasks);
        return allKlineData.SelectMany(result => result);
    }

    private async Task<IEnumerable<KlineDataNew>> CollectKlineDataForCoin(
        Coin mainCoin,
        IEnumerable<Coin> quoteCoinsPrioritization,
        KlineDataRequest baseRequest
    )
    {
        foreach (var quoteCoin in quoteCoinsPrioritization)
        {
            var request = Mapping.ToKlineDataRequest(mainCoin, baseRequest, quoteCoin);

            var klineData = await _externalClient.GetKlineData(request);
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

    private static int? GetTradingPairId(
        IEnumerable<TradingPair> existingTradingPairs,
        Coin quoteCoin
    ) => existingTradingPairs.FirstOrDefault(tp => AreCoinsEquivalent(tp.CoinQuote, quoteCoin))?.Id;

    private static bool AreCoinsEquivalent(TradingPairCoinQuote coin, Coin quoteCoin) =>
        coin.Symbol.Equals(quoteCoin.Symbol, StringComparison.OrdinalIgnoreCase)
        && coin.Name.Equals(quoteCoin.Name, StringComparison.OrdinalIgnoreCase);

    private static class Mapping
    {
        public static KlineDataRequest ToKlineDataRequest(
            Coin mainCoin,
            KlineDataRequest baseRequest,
            Coin quoteCoin
        ) =>
            new()
            {
                CoinMainSymbol = mainCoin.Symbol,
                CoinQuoteSymbol = quoteCoin.Symbol,
                Interval = baseRequest.Interval,
                StartTime = baseRequest.StartTime,
                EndTime = baseRequest.EndTime,
                Limit = baseRequest.Limit,
            };

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
    }
}
