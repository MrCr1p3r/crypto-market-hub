using GUI_Crypto.Clients.Interfaces;
using GUI_Crypto.Models.Chart;
using GUI_Crypto.Models.Input;
using GUI_Crypto.ViewModels.Factories.Interfaces;
using SharedLibrary.Enums;
using Output = GUI_Crypto.Models.Output;
using Overview = GUI_Crypto.Models.Overview;

namespace GUI_Crypto.ViewModels.Factories;

/// <summary>
/// Factory for creating view models related to cryptocurrencies.
/// </summary>
public class CryptoViewModelFactory(
    ISvcCoinsClient svcCoinsClient,
    ISvcKlineClient svcKlineClient,
    ISvcExternalClient svcExternalClient
) : ICryptoViewModelFactory
{
    private readonly ISvcCoinsClient _svcCoinsHttpClient = svcCoinsClient;
    private readonly ISvcKlineClient _svcKlineHttpClient = svcKlineClient;
    private readonly ISvcExternalClient _svcExternalHttpClient = svcExternalClient;

    /// <inheritdoc/>
    public async Task<OverviewViewModel> CreateOverviewViewModel()
    {
        var coinsTask = _svcCoinsHttpClient.GetAllCoins();
        var klineDataTask = _svcKlineHttpClient.GetAllKlineData();
        await Task.WhenAll(coinsTask, klineDataTask);

        var coins = await coinsTask;
        var klineData = await klineDataTask;
        var overviewCoins = coins.Select(coin => Mapping.ToOverviewCoin(coin, klineData));
        return new OverviewViewModel { Coins = overviewCoins };
    }

    /// <inheritdoc/>
    public async Task<ChartViewModel> CreateChartViewModel(CoinChartRequest coin)
    {
        var coinMainTask = _svcCoinsHttpClient.GetCoinsByIds([coin.IdCoinMain]);
        var klineDataTask = _svcExternalHttpClient.GetKlineData(Mapping.ToKlineDataRequest(coin));
        await Task.WhenAll(coinMainTask, klineDataTask);

        var coinMain = await coinMainTask;
        var klineData = await klineDataTask;
        var chartCoin = Mapping.ToChartCoin(coinMain.First(), klineData, coin.SymbolCoinQuote);

        return new ChartViewModel { Coin = chartCoin };
    }

    private static class Mapping
    {
        public static Overview.OverviewCoin ToOverviewCoin(
            Output.Coin coin,
            IReadOnlyDictionary<int, IEnumerable<Output.KlineData>> klineDataDict
        )
        {
            var tradingPair = coin.TradingPairs.FirstOrDefault(tradingPair =>
                klineDataDict.ContainsKey(tradingPair.Id)
            );
            var klineData = tradingPair != null ? klineDataDict[tradingPair.Id] : [];

            return new Overview.OverviewCoin
            {
                Id = coin.Id,
                Symbol = coin.Symbol,
                Name = coin.Name,
                TradingPair = tradingPair,
                KlineData = klineData,
            };
        }

        public static KlineDataRequest ToKlineDataRequest(CoinChartRequest coin) =>
            new()
            {
                CoinMainSymbol = coin.SymbolCoinMain,
                CoinQuoteSymbol = coin.SymbolCoinQuote,
                Interval = ExchangeKlineInterval.FifteenMinutes,
                StartTime = DateTime.UtcNow.AddDays(-7),
                EndTime = DateTime.UtcNow,
            };

        public static CoinChart ToChartCoin(
            Output.Coin coinMain,
            IEnumerable<Output.KlineDataExchange> klineData,
            string symbolCoinQuote
        ) =>
            new()
            {
                Id = coinMain.Id,
                Symbol = coinMain.Symbol,
                Name = coinMain.Name,
                TradingPairs = coinMain.TradingPairs,
                SymbolCoinQuoteCurrent = symbolCoinQuote,
                KlineData = klineData,
            };
    }
}
