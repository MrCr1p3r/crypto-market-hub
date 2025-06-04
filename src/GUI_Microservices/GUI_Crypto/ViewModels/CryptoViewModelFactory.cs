using GUI_Crypto.ServiceModels;
using GUI_Crypto.ViewModels.Chart;
using GUI_Crypto.ViewModels.Chart.Models;
using SvcCoins = GUI_Crypto.MicroserviceClients.SvcCoins.Contracts.Responses;
using SvcExternal = GUI_Crypto.MicroserviceClients.SvcExternal.Contracts.Responses.KlineData;

namespace GUI_Crypto.ViewModels;

/// <summary>
/// Factory for creating view models related to cryptocurrencies.
/// </summary>
public class CryptoViewModelFactory : ICryptoViewModelFactory
{
    /// <inheritdoc/>
    public ChartViewModel CreateChartViewModel(ChartData data)
    {
        var coinChart = Mapping.ToCoinChart(data);

        return new ChartViewModel { Coin = coinChart };
    }

    /// <summary>
    /// Internal mapping class for converting between different data models.
    /// </summary>
    private static class Mapping
    {
        /// <summary>
        /// Converts chart data to a coin chart view model.
        /// </summary>
        public static CoinChart ToCoinChart(ChartData data) =>
            new()
            {
                Id = data.Coin.Id,
                Symbol = data.Coin.Symbol,
                Name = data.Coin.Name,
                TradingPairs = ToCoinChartTradingPairs(data.Coin.TradingPairs),
                SelectedQuoteCoinSymbol = data
                    .Coin.TradingPairs.First(tp => tp.Id == data.KlineResponse.IdTradingPair)
                    .CoinQuote.Symbol,
                KlineData = ToCoinChartKlineData(data.KlineResponse.KlineData),
            };

        private static IEnumerable<TradingPair> ToCoinChartTradingPairs(
            IEnumerable<SvcCoins.TradingPair> tradingPairs
        ) =>
            tradingPairs.Select(tp => new TradingPair
            {
                Id = tp.Id,
                CoinQuote = new TradingPairCoinQuote
                {
                    Id = tp.CoinQuote.Id,
                    Symbol = tp.CoinQuote.Symbol,
                    Name = tp.CoinQuote.Name,
                },
                Exchanges = tp.Exchanges,
            });

        private static IEnumerable<KlineData> ToCoinChartKlineData(
            IEnumerable<SvcExternal.KlineData> klineData
        ) =>
            klineData.Select(kline => new KlineData
            {
                OpenTime = kline.OpenTime,
                OpenPrice = kline.OpenPrice,
                HighPrice = kline.HighPrice,
                LowPrice = kline.LowPrice,
                ClosePrice = kline.ClosePrice,
                Volume = kline.Volume,
                CloseTime = kline.CloseTime,
            });
    }
}
