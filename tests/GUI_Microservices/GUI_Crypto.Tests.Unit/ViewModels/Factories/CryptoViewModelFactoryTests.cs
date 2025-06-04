using GUI_Crypto.ServiceModels;
using GUI_Crypto.ViewModels;
using GUI_Crypto.ViewModels.Chart.Models;
using SharedLibrary.Enums;
using SvcCoins = GUI_Crypto.MicroserviceClients.SvcCoins.Contracts.Responses;
using SvcExternal = GUI_Crypto.MicroserviceClients.SvcExternal.Contracts.Responses.KlineData;

namespace GUI_Crypto.Tests.Unit.ViewModels.Factories;

public class CryptoViewModelFactoryTests
{
    private readonly CryptoViewModelFactory _factory;

    public CryptoViewModelFactoryTests()
    {
        _factory = new CryptoViewModelFactory();
    }

    #region CreateChartViewModel Tests

    [Fact]
    public void CreateChartViewModel_WhenValidChartData_ShouldReturnCorrectlyMappedViewModel()
    {
        // Arrange
        var chartData = TestData.BitcoinChartData;

        // Act
        var result = _factory.CreateChartViewModel(chartData);

        // Assert
        result.Should().NotBeNull();
        result.Coin.Should().NotBeNull();

        var coinChart = result.Coin;
        coinChart.Id.Should().Be(1);
        coinChart.Symbol.Should().Be("BTC");
        coinChart.Name.Should().Be("Bitcoin");
        coinChart.SelectedQuoteCoinSymbol.Should().Be("USDT");
        coinChart.TradingPairs.Should().HaveCount(2);
        coinChart.KlineData.Should().HaveCount(2);

        // Verify trading pairs mapping
        var tradingPairs = coinChart.TradingPairs.ToList();
        tradingPairs[0].Id.Should().Be(101);
        tradingPairs[0].CoinQuote.Symbol.Should().Be("USDT");
        tradingPairs[0].Exchanges.Should().BeEquivalentTo([Exchange.Binance]);

        tradingPairs[1].Id.Should().Be(102);
        tradingPairs[1].CoinQuote.Symbol.Should().Be("BTC");
        tradingPairs[1].Exchanges.Should().BeEquivalentTo([Exchange.Bybit]);

        // Verify kline data mapping
        var klineData = coinChart.KlineData.ToList();
        klineData[0].Should().BeEquivalentTo(TestData.ExpectedFirstKlineData);
        klineData[1].Should().BeEquivalentTo(TestData.ExpectedSecondKlineData);
    }

    [Fact]
    public void CreateChartViewModel_WhenMultipleTradingPairs_ShouldSelectCorrectQuoteCoin()
    {
        // Arrange
        var chartData = TestData.EthereumChartDataWithMultiplePairs;

        // Act
        var result = _factory.CreateChartViewModel(chartData);

        // Assert
        result.Coin.SelectedQuoteCoinSymbol.Should().Be("BTC");
        result.Coin.TradingPairs.Should().HaveCount(3);

        // Verify the selected quote coin matches the kline response trading pair
        var selectedTradingPair = result.Coin.TradingPairs.First(tp =>
            tp.CoinQuote.Symbol == result.Coin.SelectedQuoteCoinSymbol
        );
        selectedTradingPair.Id.Should().Be(TestData.EthereumKlineResponse.IdTradingPair);
    }

    [Fact]
    public void CreateChartViewModel_WhenEmptyKlineData_ShouldReturnEmptyKlineCollection()
    {
        // Arrange
        var chartData = TestData.BitcoinChartDataWithEmptyKline;

        // Act
        var result = _factory.CreateChartViewModel(chartData);

        // Assert
        result.Coin.KlineData.Should().BeEmpty();
        result.Coin.SelectedQuoteCoinSymbol.Should().Be("USDT");
        result.Coin.TradingPairs.Should().HaveCount(1);
    }

    [Fact]
    public void CreateChartViewModel_WhenSingleTradingPair_ShouldMapCorrectly()
    {
        // Arrange
        var chartData = TestData.AdaChartDataWithSinglePair;

        // Act
        var result = _factory.CreateChartViewModel(chartData);

        // Assert
        result.Coin.TradingPairs.Should().HaveCount(1);
        result.Coin.SelectedQuoteCoinSymbol.Should().Be("USDT");

        var tradingPair = result.Coin.TradingPairs.First();
        tradingPair.Id.Should().Be(103);
        tradingPair.CoinQuote.Id.Should().Be(5);
        tradingPair.CoinQuote.Symbol.Should().Be("USDT");
        tradingPair.CoinQuote.Name.Should().Be("Tether");
        tradingPair.Exchanges.Should().BeEquivalentTo([Exchange.Binance, Exchange.Bybit]);
    }

    [Fact]
    public void CreateChartViewModel_WhenStablecoinData_ShouldMapCorrectly()
    {
        // Arrange
        var chartData = TestData.UsdtStablecoinChartData;

        // Act
        var result = _factory.CreateChartViewModel(chartData);

        // Assert
        result.Coin.Id.Should().Be(5);
        result.Coin.Symbol.Should().Be("USDT");
        result.Coin.Name.Should().Be("Tether");
        result.Coin.SelectedQuoteCoinSymbol.Should().Be("USD");
        result.Coin.TradingPairs.Should().HaveCount(1);
        result.Coin.KlineData.Should().HaveCount(1);
    }

    [Fact]
    public void CreateChartViewModel_WhenComplexKlineData_ShouldMapAllProperties()
    {
        // Arrange
        var chartData = TestData.ComplexKlineChartData;

        // Act
        var result = _factory.CreateChartViewModel(chartData);

        // Assert
        var klineData = result.Coin.KlineData.ToList();
        klineData.Should().HaveCount(3);

        // Verify detailed mapping of first kline data point
        klineData[0].OpenTime.Should().Be(1640995200000);
        klineData[0].OpenPrice.Should().Be(46000.50m);
        klineData[0].HighPrice.Should().Be(47000.75m);
        klineData[0].LowPrice.Should().Be(45500.25m);
        klineData[0].ClosePrice.Should().Be(46800.00m);
        klineData[0].Volume.Should().Be(123.456m);
        klineData[0].CloseTime.Should().Be(1640998800000);

        // Verify second kline data point with different values
        klineData[1].OpenTime.Should().Be(1640998800000);
        klineData[1].OpenPrice.Should().Be(46800.00m);
        klineData[1].HighPrice.Should().Be(48000.00m);
        klineData[1].LowPrice.Should().Be(46500.00m);
        klineData[1].ClosePrice.Should().Be(47500.50m);
        klineData[1].Volume.Should().Be(234.567m);
        klineData[1].CloseTime.Should().Be(1641002400000);
    }

    #endregion

    private static class TestData
    {
        public static readonly SvcCoins.Coin BitcoinCoin = new()
        {
            Id = 1,
            Symbol = "BTC",
            Name = "Bitcoin",
            Category = null,
            IdCoinGecko = "bitcoin",
            TradingPairs =
            [
                new SvcCoins.TradingPair
                {
                    Id = 101,
                    CoinQuote = new SvcCoins.TradingPairCoinQuote
                    {
                        Id = 5,
                        Symbol = "USDT",
                        Name = "Tether",
                    },
                    Exchanges = [Exchange.Binance],
                },
                new SvcCoins.TradingPair
                {
                    Id = 102,
                    CoinQuote = new SvcCoins.TradingPairCoinQuote
                    {
                        Id = 1,
                        Symbol = "BTC",
                        Name = "Bitcoin",
                    },
                    Exchanges = [Exchange.Bybit],
                },
            ],
        };

        public static readonly SvcExternal.KlineDataResponse BitcoinKlineResponse = new()
        {
            IdTradingPair = 101,
            KlineData =
            [
                new SvcExternal.KlineData
                {
                    OpenTime = 1640995200000,
                    OpenPrice = 46000.50m,
                    HighPrice = 47000.75m,
                    LowPrice = 45500.25m,
                    ClosePrice = 46800.00m,
                    Volume = 123.456m,
                    CloseTime = 1640998800000,
                },
                new SvcExternal.KlineData
                {
                    OpenTime = 1640998800000,
                    OpenPrice = 46800.00m,
                    HighPrice = 48000.00m,
                    LowPrice = 46500.00m,
                    ClosePrice = 47500.50m,
                    Volume = 234.567m,
                    CloseTime = 1641002400000,
                },
            ],
        };

        public static readonly ChartData BitcoinChartData = new()
        {
            Coin = BitcoinCoin,
            KlineResponse = BitcoinKlineResponse,
        };

        public static readonly SvcCoins.Coin EthereumCoin = new()
        {
            Id = 2,
            Symbol = "ETH",
            Name = "Ethereum",
            Category = null,
            IdCoinGecko = "ethereum",
            TradingPairs =
            [
                new SvcCoins.TradingPair
                {
                    Id = 201,
                    CoinQuote = new SvcCoins.TradingPairCoinQuote
                    {
                        Id = 5,
                        Symbol = "USDT",
                        Name = "Tether",
                    },
                    Exchanges = [Exchange.Binance],
                },
                new SvcCoins.TradingPair
                {
                    Id = 202,
                    CoinQuote = new SvcCoins.TradingPairCoinQuote
                    {
                        Id = 1,
                        Symbol = "BTC",
                        Name = "Bitcoin",
                    },
                    Exchanges = [Exchange.Bybit],
                },
                new SvcCoins.TradingPair
                {
                    Id = 203,
                    CoinQuote = new SvcCoins.TradingPairCoinQuote
                    {
                        Id = 6,
                        Symbol = "EUR",
                        Name = "Euro",
                    },
                    Exchanges = [Exchange.Binance, Exchange.Bybit],
                },
            ],
        };

        public static readonly SvcExternal.KlineDataResponse EthereumKlineResponse = new()
        {
            IdTradingPair = 202, // ETH/BTC pair
            KlineData =
            [
                new SvcExternal.KlineData
                {
                    OpenTime = 1640995200000,
                    OpenPrice = 0.075m,
                    HighPrice = 0.078m,
                    LowPrice = 0.074m,
                    ClosePrice = 0.076m,
                    Volume = 50.123m,
                    CloseTime = 1640998800000,
                },
            ],
        };

        public static readonly ChartData EthereumChartDataWithMultiplePairs = new()
        {
            Coin = EthereumCoin,
            KlineResponse = EthereumKlineResponse,
        };

        public static readonly SvcExternal.KlineDataResponse EmptyKlineResponse = new()
        {
            IdTradingPair = 101,
            KlineData = [],
        };

        public static readonly ChartData BitcoinChartDataWithEmptyKline = new()
        {
            Coin = new SvcCoins.Coin
            {
                Id = 1,
                Symbol = "BTC",
                Name = "Bitcoin",
                Category = null,
                TradingPairs =
                [
                    new SvcCoins.TradingPair
                    {
                        Id = 101,
                        CoinQuote = new SvcCoins.TradingPairCoinQuote
                        {
                            Id = 5,
                            Symbol = "USDT",
                            Name = "Tether",
                        },
                        Exchanges = [Exchange.Binance],
                    },
                ],
            },
            KlineResponse = EmptyKlineResponse,
        };

        public static readonly SvcCoins.Coin AdaCoin = new()
        {
            Id = 3,
            Symbol = "ADA",
            Name = "Cardano",
            Category = null,
            IdCoinGecko = "cardano",
            TradingPairs =
            [
                new SvcCoins.TradingPair
                {
                    Id = 103,
                    CoinQuote = new SvcCoins.TradingPairCoinQuote
                    {
                        Id = 5,
                        Symbol = "USDT",
                        Name = "Tether",
                    },
                    Exchanges = [Exchange.Binance, Exchange.Bybit],
                },
            ],
        };

        public static readonly SvcExternal.KlineDataResponse AdaKlineResponse = new()
        {
            IdTradingPair = 103,
            KlineData =
            [
                new SvcExternal.KlineData
                {
                    OpenTime = 1640995200000,
                    OpenPrice = 1.25m,
                    HighPrice = 1.30m,
                    LowPrice = 1.20m,
                    ClosePrice = 1.28m,
                    Volume = 1000.789m,
                    CloseTime = 1640998800000,
                },
            ],
        };

        public static readonly ChartData AdaChartDataWithSinglePair = new()
        {
            Coin = AdaCoin,
            KlineResponse = AdaKlineResponse,
        };

        public static readonly SvcCoins.Coin UsdtStablecoin = new()
        {
            Id = 5,
            Symbol = "USDT",
            Name = "Tether",
            Category = CoinCategory.Stablecoin,
            IdCoinGecko = "tether",
            TradingPairs =
            [
                new SvcCoins.TradingPair
                {
                    Id = 501,
                    CoinQuote = new SvcCoins.TradingPairCoinQuote
                    {
                        Id = 7,
                        Symbol = "USD",
                        Name = "US Dollar",
                    },
                    Exchanges = [Exchange.Binance],
                },
            ],
        };

        public static readonly SvcExternal.KlineDataResponse UsdtKlineResponse = new()
        {
            IdTradingPair = 501,
            KlineData =
            [
                new SvcExternal.KlineData
                {
                    OpenTime = 1640995200000,
                    OpenPrice = 1.0001m,
                    HighPrice = 1.0002m,
                    LowPrice = 0.9999m,
                    ClosePrice = 1.0000m,
                    Volume = 10000.0m,
                    CloseTime = 1640998800000,
                },
            ],
        };

        public static readonly ChartData UsdtStablecoinChartData = new()
        {
            Coin = UsdtStablecoin,
            KlineResponse = UsdtKlineResponse,
        };

        public static readonly SvcExternal.KlineDataResponse ComplexKlineResponse = new()
        {
            IdTradingPair = 101,
            KlineData =
            [
                new SvcExternal.KlineData
                {
                    OpenTime = 1640995200000,
                    OpenPrice = 46000.50m,
                    HighPrice = 47000.75m,
                    LowPrice = 45500.25m,
                    ClosePrice = 46800.00m,
                    Volume = 123.456m,
                    CloseTime = 1640998800000,
                },
                new SvcExternal.KlineData
                {
                    OpenTime = 1640998800000,
                    OpenPrice = 46800.00m,
                    HighPrice = 48000.00m,
                    LowPrice = 46500.00m,
                    ClosePrice = 47500.50m,
                    Volume = 234.567m,
                    CloseTime = 1641002400000,
                },
                new SvcExternal.KlineData
                {
                    OpenTime = 1641002400000,
                    OpenPrice = 47500.50m,
                    HighPrice = 49000.00m,
                    LowPrice = 47000.00m,
                    ClosePrice = 48750.25m,
                    Volume = 345.678m,
                    CloseTime = 1641006000000,
                },
            ],
        };

        public static readonly ChartData ComplexKlineChartData = new()
        {
            Coin = BitcoinCoin,
            KlineResponse = ComplexKlineResponse,
        };

        // Expected results for verification
        public static readonly KlineData ExpectedFirstKlineData = new()
        {
            OpenTime = 1640995200000,
            OpenPrice = 46000.50m,
            HighPrice = 47000.75m,
            LowPrice = 45500.25m,
            ClosePrice = 46800.00m,
            Volume = 123.456m,
            CloseTime = 1640998800000,
        };

        public static readonly KlineData ExpectedSecondKlineData = new()
        {
            OpenTime = 1640998800000,
            OpenPrice = 46800.00m,
            HighPrice = 48000.00m,
            LowPrice = 46500.00m,
            ClosePrice = 47500.50m,
            Volume = 234.567m,
            CloseTime = 1641002400000,
        };
    }
}
