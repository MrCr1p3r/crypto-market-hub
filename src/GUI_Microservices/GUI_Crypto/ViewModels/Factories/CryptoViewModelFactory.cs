using GUI_Crypto.Clients.Interfaces;
using GUI_Crypto.ViewModels.Factories.Interfaces;
using Output = GUI_Crypto.Models.Output;
using Overview = GUI_Crypto.Models.Overview;

namespace GUI_Crypto.ViewModels.Factories;

/// <summary>
/// Factory for creating view models related to cryptocurrencies.
/// </summary>
public class CryptoViewModelFactory(ISvcCoinsClient svcCoinsClient, ISvcKlineClient svcKlineClient)
    : ICryptoViewModelFactory
{
    private readonly ISvcCoinsClient _svcCoinsHttpClient = svcCoinsClient;
    private readonly ISvcKlineClient _svcKlineHttpClient = svcKlineClient;

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

    private static class Mapping
    {
        public static Overview.Coin ToOverviewCoin(
            Output.Coin coin,
            IEnumerable<Output.KlineData> klineData
        ) =>
            new()
            {
                Id = coin.Id,
                Symbol = coin.Symbol,
                Name = coin.Name,
                KlineData = klineData.Where(kline =>
                    coin.TradingPairs.Any(pair => pair.Id == kline.IdTradePair)
                ),
            };
    }
}
