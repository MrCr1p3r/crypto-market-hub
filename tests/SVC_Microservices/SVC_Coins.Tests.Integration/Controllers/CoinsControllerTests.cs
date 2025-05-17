using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Enums;
using SVC_Coins.ApiContracts.Requests;
using SVC_Coins.ApiContracts.Requests.CoinCreation;
using SVC_Coins.ApiContracts.Responses;
using SVC_Coins.Domain.Entities;
using SVC_Coins.Tests.Integration.Factories;

namespace SVC_Coins.Tests.Integration.Controllers;

public class CoinsControllerTests(CustomWebApplicationFactory factory)
    : BaseIntegrationTest(factory),
        IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task GetCoins_WhenIdsAreNotProvided_ReturnsAllCoins()
    {
        // Arrange
        await SeedDatabase();
        var expectedCoins = new Coin[] { TestData.Btc, TestData.Eth, TestData.Usdt };

        // Act
        var response = await Client.GetAsync("/coins");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var actualCoins = await response.Content.ReadFromJsonAsync<IEnumerable<Coin>>();
        actualCoins.Should().BeEquivalentTo(expectedCoins);
    }

    [Fact]
    public async Task GetCoins_WhenIdsAreProvided_ReturnsCorrectCoins()
    {
        // Arrange
        await SeedDatabase();
        var expectedCoins = new Coin[] { TestData.Btc, TestData.Eth };

        // Act
        var response = await Client.GetAsync($"/coins?ids={TestData.BtcId}&ids={TestData.EthId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var actualCoins = await response.Content.ReadFromJsonAsync<IEnumerable<Coin>>();
        actualCoins.Should().BeEquivalentTo(expectedCoins);
    }

    [Fact]
    public async Task GetCoins_WhenSomeIdsAreNonExistent_ReturnsOnlyExistingCoins()
    {
        // Arrange
        await SeedDatabase();
        var expectedCoins = new Coin[] { TestData.Btc };

        // Act
        var response = await Client.GetAsync($"/coins?ids={TestData.BtcId}&ids=999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var actualCoins = await response.Content.ReadFromJsonAsync<IEnumerable<Coin>>();
        actualCoins.Should().BeEquivalentTo(expectedCoins);
    }

    [Fact]
    public async Task GetCoins_WhenAllIdsAreNonExistent_ReturnsEmptyList()
    {
        // Arrange
        var nonExistentId1 = int.MaxValue - 1;
        var nonExistentId2 = int.MaxValue;

        // Act
        var response = await Client.GetAsync($"/coins?ids={nonExistentId1}&ids={nonExistentId2}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var actualCoins = await response.Content.ReadFromJsonAsync<IEnumerable<Coin>>();
        actualCoins.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateCoins_WhenSuccessful_ReturnsCreatedCoins()
    {
        // Arrange
        await SeedDatabase();
        var expectedCoins = new Coin[] { TestData.Xrp, TestData.Sol };

        // Act
        var response = await Client.PostAsJsonAsync("/coins", TestData.CoinsToInsert);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var actualCoins = await response.Content.ReadFromJsonAsync<IEnumerable<Coin>>();
        actualCoins.Should().BeEquivalentTo(expectedCoins);

        using var dbContext = GetDbContext();
        var coinsList = await dbContext.Coins.ToListAsync();
        coinsList.Should().HaveCount(6);
        coinsList.Should().Contain(coin => coin.Id == TestData.XrpId && coin.Symbol == "XRP");
        coinsList.Should().Contain(coin => coin.Id == TestData.SolId && coin.Symbol == "SOL");
        coinsList.Should().Contain(coin => coin.Id == TestData.UsdcId && coin.Symbol == "USDC");

        var tradingPairsList = await dbContext.TradingPairs.ToListAsync();
        tradingPairsList.Should().HaveCount(6);
    }

    [Fact]
    public async Task CreateCoins_WhenValidationFails_ReturnsBadRequest()
    {
        // Act
        var response = await Client.PostAsJsonAsync("/coins", TestData.CoinsToInsert);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var dbContext = GetDbContext();
        (await dbContext.Coins.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task UpdateMarketData_WhenSuccessful_ReturnsUpdatedCoins()
    {
        // Arrange
        await SeedDatabase();
        var expectedEth = TestData.ExpectedCoinsWithUpdatedMarketData.First(coin =>
            coin.Id == TestData.EthId
        );

        // Act
        var response = await Client.PatchAsJsonAsync(
            "/coins/market-data",
            TestData.MarketDataUpdateRequests
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var actualCoins = await response.Content.ReadFromJsonAsync<IEnumerable<Coin>>();
        actualCoins.Should().BeEquivalentTo(TestData.ExpectedCoinsWithUpdatedMarketData);

        using var dbContext = GetDbContext();
        var ethCoin = await dbContext.Coins.FindAsync(TestData.EthId);
        ethCoin!.MarketCapUsd.Should().Be(expectedEth.MarketCapUsd);
        ethCoin.PriceUsd.Should().Be(expectedEth.PriceUsd);
        ethCoin.PriceChangePercentage24h.Should().Be(expectedEth.PriceChangePercentage24h);
    }

    [Fact]
    public async Task UpdateMarketData_WhenCoinsDoNotExist_ReturnsNotFound()
    {
        // Act
        var response = await Client.PatchAsJsonAsync(
            "/coins/market-data",
            TestData.MarketDataUpdateRequests
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReplaceTradingPairs_WhenSuccessful_ReturnsNewTradingPairs()
    {
        // Arrange
        await SeedDatabase();

        // Act
        var response = await Client.PutAsJsonAsync(
            "/coins/trading-pairs",
            TestData.NewTradingPairs
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var actualTradingPairs = await response.Content.ReadFromJsonAsync<
            IEnumerable<TradingPair>
        >();
        actualTradingPairs.Should().BeEquivalentTo(TestData.ExpectedTradingPairs);

        using var dbContext = GetDbContext();
        var tradingPairsList = await dbContext.TradingPairs.ToListAsync();
        tradingPairsList.Should().HaveCount(2);
        tradingPairsList.Should().Contain(tp => tp.Id == 4);
        tradingPairsList.Should().Contain(tp => tp.Id == 5);
    }

    [Fact]
    public async Task ReplaceTradingPairs_WhenValidationFails_ReturnsBadRequest()
    {
        // Arrange
        await SeedDatabase();
        var tradingPairs = new TradingPairCreationRequest[]
        {
            new()
            {
                IdCoinMain = 99,
                IdCoinQuote = 999,
                Exchanges = [Exchange.Binance, Exchange.Bybit],
            },
        };

        // Act
        var response = await Client.PutAsJsonAsync("/coins/trading-pairs", tradingPairs);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var dbContext = GetDbContext();
        var tradingPairsList = await dbContext.TradingPairs.ToListAsync();
        tradingPairsList.Should().HaveCount(3);
    }

    [Fact]
    public async Task DeleteCoin_WhenSuccessful_ReturnsNoContent()
    {
        // Arrange
        await SeedDatabase();

        // Act
        var response = await Client.DeleteAsync($"/coins/{TestData.BtcId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var dbContext = GetDbContext();
        var coinsList = await dbContext.Coins.ToListAsync();
        coinsList.Should().HaveCount(2);
        coinsList.Should().NotContain(coin => coin.Id == TestData.BtcId);
    }

    [Fact]
    public async Task DeleteCoin_WhenCoinDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = int.MaxValue;

        // Act
        var response = await Client.DeleteAsync($"/coins/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteUnreferencedCoins_WhenSuccessful_ReturnsNoContent()
    {
        // Arrange
        await SeedDatabase();

        // Act
        var response = await Client.DeleteAsync("/coins/unreferenced");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var dbContext = GetDbContext();
        (await dbContext.Coins.CountAsync()).Should().Be(3);
        (await dbContext.TradingPairs.CountAsync()).Should().Be(3);
        (await dbContext.Exchanges.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task DeleteAllCoins_WhenSuccessful_ReturnsNoContent()
    {
        // Arrange
        await SeedDatabase();

        // Act
        var response = await Client.DeleteAsync("/coins");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var dbContext = GetDbContext();
        (await dbContext.Coins.CountAsync()).Should().Be(0);
        (await dbContext.TradingPairs.CountAsync()).Should().Be(0);
        (await dbContext.Exchanges.CountAsync()).Should().Be(2);
    }

    private async Task SeedDatabase()
    {
        using var dbContext = GetDbContext();

        // 1. Create and Add Exchange Entities
        var binanceExchange = new ExchangesEntity { Name = "Binance" };
        var bybitExchange = new ExchangesEntity { Name = "Bybit" };
        dbContext.Exchanges.AddRange(binanceExchange, bybitExchange);

        // 2. Create and Add Coin Entities
        var btcCoin = new CoinsEntity
        {
            Name = "Bitcoin",
            Symbol = "BTC",
            IdCoinGecko = "coingecko-bitcoin",
            MarketCapUsd = 1000000000,
            PriceUsd = "50000",
            PriceChangePercentage24h = -10,
        };
        var ethCoin = new CoinsEntity { Name = "Ethereum", Symbol = "ETH" };
        var usdtCoin = new CoinsEntity
        {
            Name = "Tether",
            Symbol = "USDT",
            IsStablecoin = true,
        };
        dbContext.Coins.AddRange(btcCoin, ethCoin, usdtCoin);

        // 3. Create and Add TradingPair Entities
        var btcUsdtPair = new TradingPairsEntity
        {
            IdCoinMain = btcCoin.Id,
            CoinMain = btcCoin,
            IdCoinQuote = usdtCoin.Id,
            CoinQuote = usdtCoin,
            Exchanges = [binanceExchange, bybitExchange],
        };
        var btcEthPair = new TradingPairsEntity
        {
            IdCoinMain = btcCoin.Id,
            CoinMain = btcCoin,
            IdCoinQuote = ethCoin.Id,
            CoinQuote = ethCoin,
            Exchanges = [binanceExchange],
        };
        var ethUsdtPair = new TradingPairsEntity
        {
            IdCoinMain = ethCoin.Id,
            CoinMain = ethCoin,
            IdCoinQuote = usdtCoin.Id,
            CoinQuote = usdtCoin,
            Exchanges = [binanceExchange],
        };
        dbContext.TradingPairs.AddRange(btcUsdtPair, btcEthPair, ethUsdtPair);

        // 4. Save all changes in a single transaction
        await dbContext.SaveChangesAsync();
    }

    private static class TestData
    {
        // Define constants for IDs to ensure consistency
        public const int BtcId = 1;
        public const int EthId = 2;
        public const int UsdtId = 3;
        public const int BtcUsdtPairId = 1;
        public const int BtcEthPairId = 2;
        public const int EthUsdtPairId = 3;

        // Ids of coins and trading pairs inserted through endpoint
        public const int XrpId = 4;
        public const int SolId = 5;
        public const int UsdcId = 6;
        public const int XrpUsdcPairId = 4;
        public const int XrpBtcPairId = 5;
        public const int SolUsdcPairId = 6;

        public static readonly Coin Btc = new()
        {
            Id = BtcId,
            Name = "Bitcoin",
            Symbol = "BTC",
            IdCoinGecko = "coingecko-bitcoin",
            MarketCapUsd = 1000000000,
            PriceUsd = "50000",
            PriceChangePercentage24h = -10,
            TradingPairs =
            [
                new()
                {
                    Id = BtcUsdtPairId,
                    CoinQuote = new TradingPairCoinQuote
                    {
                        Id = UsdtId,
                        Name = "Tether",
                        Symbol = "USDT",
                        Category = CoinCategory.Stablecoin,
                    },
                    Exchanges = [Exchange.Binance, Exchange.Bybit],
                },
                new()
                {
                    Id = BtcEthPairId,
                    CoinQuote = new TradingPairCoinQuote
                    {
                        Id = EthId,
                        Name = "Ethereum",
                        Symbol = "ETH",
                    },
                    Exchanges = [Exchange.Binance],
                },
            ],
        };

        public static readonly Coin Eth = new()
        {
            Id = EthId,
            Name = "Ethereum",
            Symbol = "ETH",
            TradingPairs =
            [
                new()
                {
                    Id = EthUsdtPairId,
                    CoinQuote = new TradingPairCoinQuote
                    {
                        Id = UsdtId,
                        Name = "Tether",
                        Symbol = "USDT",
                        Category = CoinCategory.Stablecoin,
                    },
                    Exchanges = [Exchange.Binance],
                },
            ],
        };

        public static readonly Coin Usdt = new()
        {
            Id = UsdtId,
            Name = "Tether",
            Symbol = "USDT",
            Category = CoinCategory.Stablecoin,
            TradingPairs = [],
        };

        public static readonly IEnumerable<CoinCreationRequest> CoinsToInsert =
        [
            new()
            {
                Name = "Ripple",
                Symbol = "XRP",
                IdCoinGecko = "coingecko-xrp",
                TradingPairs =
                [
                    new()
                    {
                        CoinQuote = new CoinCreationCoinQuote
                        {
                            Name = "USD Coin",
                            Symbol = "USDC",
                            Category = CoinCategory.Stablecoin,
                        },
                        Exchanges = [Exchange.Binance, Exchange.Bybit],
                    },
                    new()
                    {
                        CoinQuote = new CoinCreationCoinQuote
                        {
                            Id = BtcId,
                            Symbol = "BTC",
                            Name = "Bitcoin",
                        },
                        Exchanges = [Exchange.Binance],
                    },
                ],
            },
            new()
            {
                Name = "Solana",
                Symbol = "SOL",
                IdCoinGecko = "coingecko-solana",
                TradingPairs =
                [
                    new()
                    {
                        CoinQuote = new CoinCreationCoinQuote
                        {
                            Name = "USD Coin",
                            Symbol = "USDC",
                            Category = CoinCategory.Stablecoin,
                        },
                        Exchanges = [Exchange.Binance],
                    },
                ],
            },
        ];

        public static readonly Coin Xrp = new()
        {
            Id = XrpId,
            Name = "Ripple",
            Symbol = "XRP",
            IdCoinGecko = "coingecko-xrp",
            TradingPairs =
            [
                new()
                {
                    Id = XrpUsdcPairId,
                    CoinQuote = new TradingPairCoinQuote
                    {
                        Id = UsdcId,
                        Name = "USD Coin",
                        Symbol = "USDC",
                        Category = CoinCategory.Stablecoin,
                    },
                    Exchanges = [Exchange.Binance, Exchange.Bybit],
                },
                new()
                {
                    Id = XrpBtcPairId,
                    CoinQuote = new TradingPairCoinQuote
                    {
                        Id = BtcId,
                        Name = "Bitcoin",
                        Symbol = "BTC",
                        IdCoinGecko = "coingecko-bitcoin",
                        MarketCapUsd = 1000000000,
                        PriceUsd = "50000",
                        PriceChangePercentage24h = -10,
                    },
                    Exchanges = [Exchange.Binance],
                },
            ],
        };

        public static readonly Coin Sol = new()
        {
            Id = SolId,
            Name = "Solana",
            Symbol = "SOL",
            IdCoinGecko = "coingecko-solana",
            TradingPairs =
            [
                new()
                {
                    Id = SolUsdcPairId,
                    CoinQuote = new TradingPairCoinQuote
                    {
                        Id = UsdcId,
                        Name = "USD Coin",
                        Symbol = "USDC",
                        Category = CoinCategory.Stablecoin,
                    },
                    Exchanges = [Exchange.Binance],
                },
            ],
        };

        public static readonly IEnumerable<CoinMarketDataUpdateRequest> MarketDataUpdateRequests =
        [
            new()
            {
                Id = BtcId,
                MarketCapUsd = 1000,
                PriceUsd = 49999,
                PriceChangePercentage24h = 3000,
            },
            new()
            {
                Id = EthId,
                MarketCapUsd = 1000000000,
                PriceUsd = 40000,
                PriceChangePercentage24h = -20,
            },
        ];

        public static readonly IEnumerable<Coin> ExpectedCoinsWithUpdatedMarketData =
        [
            new()
            {
                Id = BtcId,
                Name = "Bitcoin",
                Symbol = "BTC",
                IdCoinGecko = "coingecko-bitcoin",
                MarketCapUsd = 1000,
                PriceUsd = "49999",
                PriceChangePercentage24h = 3000,
            },
            new()
            {
                Id = EthId,
                Name = "Ethereum",
                Symbol = "ETH",
                MarketCapUsd = 1000000000,
                PriceUsd = "40000",
                PriceChangePercentage24h = -20,
            },
        ];

        public static readonly IEnumerable<TradingPairCreationRequest> NewTradingPairs =
        [
            new()
            {
                IdCoinMain = BtcId,
                IdCoinQuote = EthId,
                Exchanges = [Exchange.Binance, Exchange.Bybit],
            },
            new()
            {
                IdCoinMain = EthId,
                IdCoinQuote = BtcId,
                Exchanges = [Exchange.Binance, Exchange.Bybit],
            },
        ];

        public static readonly IEnumerable<TradingPair> ExpectedTradingPairs =
        [
            new()
            {
                Id = 4,
                CoinQuote = new TradingPairCoinQuote
                {
                    Id = EthId,
                    Name = "Ethereum",
                    Symbol = "ETH",
                },
                Exchanges = [Exchange.Binance, Exchange.Bybit],
            },
            new()
            {
                Id = 5,
                CoinQuote = new TradingPairCoinQuote
                {
                    Id = BtcId,
                    Name = "Bitcoin",
                    Symbol = "BTC",
                    IdCoinGecko = "coingecko-bitcoin",
                    MarketCapUsd = 1000000000,
                    PriceUsd = "50000",
                    PriceChangePercentage24h = -10,
                },
                Exchanges = [Exchange.Binance, Exchange.Bybit],
            },
        ];
    }
}
