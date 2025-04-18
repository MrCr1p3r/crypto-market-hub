using AutoFixture;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SVC_Coins.Domain.Entities;
using SVC_Coins.Domain.ValueObjects;
using SVC_Coins.Infrastructure;
using SVC_Coins.Repositories;

namespace SVC_Coins.Tests.Unit.Repositories;

public class CoinsRepositoryTests : IDisposable
{
    private readonly CoinsDbContext _context;
    private readonly CoinsRepository _testedRepository;
    private readonly Fixture _fixture;

    public CoinsRepositoryTests()
    {
        _fixture = new Fixture();
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior(1));

        var options = new DbContextOptionsBuilder<CoinsDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        _context = new CoinsDbContext(options);
        _context.Database.OpenConnection();
        _context.Database.EnsureCreated();

        _testedRepository = new CoinsRepository(_context);
    }

    [Fact]
    public async Task GetAllCoins_ReturnsAllCoinsWithTradingPairs()
    {
        // Arrange
        await _context.Coins.AddRangeAsync(TestData.Coins);
        await _context.Exchanges.AddRangeAsync(TestData.Exchanges);
        await _context.TradingPairs.AddRangeAsync(TestData.TradingPairs);
        await _context.SaveChangesAsync();

        // Act
        var result = await _testedRepository.GetAllCoins();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);

        // Verify all coins exist with correct IDs
        result.Should().Contain(c => c.Id == TestData.Coins.ElementAt(0).Id);
        result.Should().Contain(c => c.Id == TestData.Coins.ElementAt(1).Id);
        result.Should().Contain(c => c.Id == TestData.Coins.ElementAt(2).Id);

        // Verify important properties for first coin
        var firstCoin = result.First(c => c.Id == TestData.Coins.First().Id);
        firstCoin.Symbol.Should().Be("BTC");
        firstCoin.Name.Should().Be("Bitcoin");
        firstCoin.TradingPairs.Should().HaveCount(1);

        // Verify trading pair details for first coin
        var tradingPair = firstCoin.TradingPairs.First();
        tradingPair.CoinQuote.Id.Should().Be(TestData.Coins.Last().Id);
        tradingPair.Exchanges.Should().HaveCount(1);
        tradingPair.Exchanges.First().Name.Should().Be(TestData.Exchanges.First().Name);

        // Verify second coin's trading pairs
        var secondCoin = result.First(c => c.Id == TestData.Coins.ElementAt(1).Id);
        secondCoin.TradingPairs.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetCoinsByIds_ReturnsCorrectCoinsWithTradingPairs()
    {
        // Arrange
        await _context.Coins.AddRangeAsync(TestData.Coins);
        await _context.Exchanges.AddRangeAsync(TestData.Exchanges);
        await _context.TradingPairs.AddRangeAsync(TestData.TradingPairs);
        await _context.SaveChangesAsync();

        var coinIds = TestData.Coins.Select(c => c.Id);

        // Act
        var result = await _testedRepository.GetCoinsByIds(coinIds);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);

        // Verify all coins exist with correct IDs
        result.Should().Contain(c => c.Id == TestData.Coins.ElementAt(0).Id);
        result.Should().Contain(c => c.Id == TestData.Coins.ElementAt(1).Id);
        result.Should().Contain(c => c.Id == TestData.Coins.ElementAt(2).Id);

        // Verify important properties for first coin
        var firstCoin = result.First(c => c.Id == TestData.Coins.First().Id);
        firstCoin.Symbol.Should().Be("BTC");
        firstCoin.Name.Should().Be("Bitcoin");
        firstCoin.TradingPairs.Should().HaveCount(1);

        // Verify trading pair details for first coin
        var tradingPair = firstCoin.TradingPairs.First();
        tradingPair.CoinQuote.Id.Should().Be(TestData.Coins.Last().Id);
        tradingPair.Exchanges.Should().HaveCount(1);
        tradingPair.Exchanges.First().Name.Should().Be(TestData.Exchanges.First().Name);

        // Verify second coin's trading pairs
        var secondCoin = result.First(c => c.Id == TestData.Coins.ElementAt(1).Id);
        secondCoin.TradingPairs.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetCoinsByIds_WhenNoCoinsExist_ReturnsEmpty()
    {
        // Act
        var result = await _testedRepository.GetCoinsByIds([1, 2]);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCoinsBySymbolNamePairs_WhenPairsExist_ReturnsMatchingCoins()
    {
        // Arrange
        await _context.Coins.AddRangeAsync(TestData.Coins);
        await _context.Exchanges.AddRangeAsync(TestData.Exchanges);
        await _context.TradingPairs.AddRangeAsync(TestData.TradingPairs);
        await _context.SaveChangesAsync();

        var pairs = TestData.Coins.Select(c => new CoinSymbolNamePair
        {
            Symbol = c.Symbol,
            Name = c.Name,
        });

        // Act
        var result = await _testedRepository.GetCoinsBySymbolNamePairs(pairs);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);

        // Verify all coins exist with correct Symbols
        result.Should().Contain(c => c.Symbol == TestData.Coins.ElementAt(0).Symbol);
        result.Should().Contain(c => c.Symbol == TestData.Coins.ElementAt(1).Symbol);
        result.Should().Contain(c => c.Symbol == TestData.Coins.ElementAt(2).Symbol);

        // Verify important properties for first coin
        var firstCoin = result.First(c => c.Id == TestData.Coins.First().Id);
        firstCoin.Symbol.Should().Be("BTC");
        firstCoin.Name.Should().Be("Bitcoin");
        firstCoin.TradingPairs.Should().HaveCount(1);

        // Verify trading pair details for first coin
        var tradingPair = firstCoin.TradingPairs.First();
        tradingPair.CoinQuote.Id.Should().Be(TestData.Coins.Last().Id);
        tradingPair.Exchanges.Should().HaveCount(1);
        tradingPair.Exchanges.First().Name.Should().Be(TestData.Exchanges.First().Name);

        // Verify second coin's trading pairs
        var secondCoin = result.First(c => c.Id == TestData.Coins.ElementAt(1).Id);
        secondCoin.TradingPairs.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetCoinsBySymbolNamePairs_WhenNoPairsMatch_ReturnsEmpty()
    {
        // Arrange
        var pairs = new List<CoinSymbolNamePair>
        {
            new() { Symbol = "ETH", Name = "Ethereum" },
            new() { Symbol = "USDT", Name = "Tether" },
        };

        // Act
        var result = await _testedRepository.GetCoinsBySymbolNamePairs(pairs);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task InsertCoins_AddsEntitiesToDatabase()
    {
        // Arrange
        var coinsToInsert = TestData.CoinsForInsertion;

        // Act
        await _testedRepository.InsertCoins(coinsToInsert);

        // Assert
        var coinsInDb = await _context.Coins.ToListAsync();
        coinsInDb.Should().HaveCount(coinsToInsert.Count());
        coinsInDb[0].Symbol.Should().Be(TestData.CoinsForInsertion.First().Symbol);
        coinsInDb[1].Symbol.Should().Be(TestData.CoinsForInsertion.Last().Symbol);
    }

    [Fact]
    public async Task InsertCoins_ReturnsInsertedCoins()
    {
        // Act
        var result = await _testedRepository.InsertCoins(TestData.CoinsForInsertion);

        // Assert
        result.Should().BeEquivalentTo(TestData.InsertedCoins);
    }

    [Fact]
    public async Task InsertCoins_WhenCollectionIsEmpty_ShouldDoNothing()
    {
        // Act
        var result = await _testedRepository.InsertCoins([]);

        // Assert
        var coinsInDb = await _context.Coins.ToListAsync();
        coinsInDb.Should().HaveCount(0);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateCoins_UpdatesCoinsDataInDatabase()
    {
        // Arrange
        await _context.Coins.AddRangeAsync(TestData.CoinsToUpdate);
        await _context.SaveChangesAsync();

        var coinsWithUpdates = TestData.CoinsToUpdate.ToList();
        foreach (var coin in coinsWithUpdates)
        {
            if (coin.Id == 1)
            {
                coin.MarketCapUsd = int.MaxValue;
                coin.PriceUsd = "100000";
                coin.PriceChangePercentage24h = 100;
            }
            else if (coin.Id == 2)
            {
                coin.MarketCapUsd = int.MaxValue;
                coin.PriceUsd = "1";
                coin.PriceChangePercentage24h = 100;
            }
        }

        // Act
        await _testedRepository.UpdateCoins(coinsWithUpdates);

        // Assert
        var coinsInDb = await _context.Coins.ToListAsync();
        coinsInDb[0].MarketCapUsd.Should().Be(int.MaxValue);
        coinsInDb[0].PriceUsd.Should().Be("100000");
        coinsInDb[0].PriceChangePercentage24h.Should().Be(100);
        coinsInDb[1].MarketCapUsd.Should().Be(int.MaxValue);
        coinsInDb[1].PriceUsd.Should().Be("1");
        coinsInDb[1].PriceChangePercentage24h.Should().Be(100);
    }

    [Fact]
    public async Task UpdateCoins_ReturnsUpdatedCoins()
    {
        // Arrange
        await _context.Coins.AddRangeAsync(TestData.CoinsToUpdate);
        await _context.SaveChangesAsync();

        var coinsWithUpdates = TestData.CoinsToUpdate.ToList();
        foreach (var coin in coinsWithUpdates)
        {
            if (coin.Id == 1)
            {
                coin.MarketCapUsd = int.MaxValue;
                coin.PriceUsd = "100000";
                coin.PriceChangePercentage24h = 100;
            }
            else if (coin.Id == 2)
            {
                coin.MarketCapUsd = int.MaxValue;
                coin.PriceUsd = "1";
                coin.PriceChangePercentage24h = 100;
            }
        }

        // Act
        var result = await _testedRepository.UpdateCoins(coinsWithUpdates);

        // Assert
        result.Should().BeEquivalentTo(TestData.UpdatedCoins);
    }

    [Fact]
    public async Task UpdateCoins_WhenCollectionIsEmpty_ShouldDoNothing()
    {
        // Act
        var result = await _testedRepository.UpdateCoins([]);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteCoin_DeletesCoinFromDatabase()
    {
        // Arrange
        await _context.Coins.AddRangeAsync(TestData.CoinsForInsertion);
        await _context.SaveChangesAsync();
        var coinToDelete = TestData.CoinsForInsertion.First();

        // Act
        await _testedRepository.DeleteCoin(coinToDelete);

        // Assert
        var coinsInDb = await _context.Coins.ToListAsync();
        coinsInDb.Should().HaveCount(1);
        coinsInDb.Should().NotContain(coin => coin.Id == coinToDelete.Id);
    }

    [Fact]
    public async Task DeleteAllCoinsWithRelatedData_DeletesAllCoinsAndTradingPairs()
    {
        // Arrange
        await _context.Coins.AddRangeAsync(TestData.Coins);
        await _context.Exchanges.AddRangeAsync(TestData.Exchanges);
        await _context.TradingPairs.AddRangeAsync(TestData.TradingPairs);
        await _context.SaveChangesAsync();

        // Act
        await _testedRepository.DeleteAllCoinsWithRelatedData();

        // Assert
        var coinsInDb = await _context.Coins.ToListAsync();
        coinsInDb.Should().BeEmpty();
        var tradingPairsInDb = await _context.TradingPairs.ToListAsync();
        tradingPairsInDb.Should().BeEmpty();
        var exchangesInDb = await _context.Exchanges.ToListAsync();
        exchangesInDb.Should().HaveCount(TestData.Exchanges.Count());
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _context.Dispose();
        }
    }

    public static class TestData
    {
        public static readonly IEnumerable<CoinsEntity> Coins =
        [
            new CoinsEntity
            {
                Id = 1,
                Symbol = "BTC",
                Name = "Bitcoin",
                IsFiat = false,
                IdCoinGecko = "bitcoin",
                IsStablecoin = false,
            },
            new CoinsEntity
            {
                Id = 2,
                Symbol = "ETH",
                Name = "Ethereum",
                IsFiat = false,
                IdCoinGecko = null,
                IsStablecoin = false,
            },
            new CoinsEntity
            {
                Id = 3,
                Symbol = "USDT",
                Name = "Tether",
                IsFiat = false,
                IdCoinGecko = "tether",
                IsStablecoin = true,
            },
        ];

        public static readonly IEnumerable<ExchangesEntity> Exchanges =
        [
            new ExchangesEntity { Id = 1, Name = "Binance" },
            new ExchangesEntity { Id = 2, Name = "Bybit" },
        ];

        public static readonly IEnumerable<TradingPairsEntity> TradingPairs =
        [
            new TradingPairsEntity
            {
                Id = 1,
                IdCoinMain = Coins.First().Id,
                IdCoinQuote = Coins.Last().Id,
                CoinMain = Coins.First(),
                CoinQuote = Coins.Last(),
                Exchanges = [Exchanges.First()],
            },
            new TradingPairsEntity
            {
                Id = 2,
                IdCoinMain = Coins.ElementAt(1).Id,
                IdCoinQuote = Coins.ElementAt(0).Id,
                CoinMain = Coins.ElementAt(1),
                CoinQuote = Coins.ElementAt(0),
                Exchanges = [Exchanges.First(), Exchanges.Last()],
            },
        ];

        public static readonly IEnumerable<CoinsEntity> CoinsForInsertion =
        [
            new CoinsEntity
            {
                Symbol = "BTC",
                Name = "Bitcoin",
                IsFiat = false,
                IdCoinGecko = "bitcoin",
                IsStablecoin = false,
            },
            new CoinsEntity
            {
                Symbol = "USDT",
                Name = "Tether",
                IsFiat = false,
                IdCoinGecko = null,
                IsStablecoin = true,
            },
        ];

        public static readonly IEnumerable<CoinsEntity> InsertedCoins =
        [
            new CoinsEntity
            {
                Id = 1,
                Symbol = "BTC",
                Name = "Bitcoin",
                IsFiat = false,
                IdCoinGecko = "bitcoin",
                IsStablecoin = false,
            },
            new CoinsEntity
            {
                Id = 2,
                Symbol = "USDT",
                Name = "Tether",
                IsFiat = false,
                IdCoinGecko = null,
                IsStablecoin = true,
            },
        ];

        public static readonly IEnumerable<CoinsEntity> CoinsToUpdate =
        [
            new CoinsEntity
            {
                Id = 1,
                Symbol = "BTC",
                Name = "Bitcoin",
                IsFiat = false,
                IdCoinGecko = "bitcoin",
                IsStablecoin = false,
            },
            new CoinsEntity
            {
                Id = 2,
                Symbol = "USDT",
                Name = "Tether",
                IsFiat = false,
                IdCoinGecko = null,
                IsStablecoin = true,
            },
        ];

        public static readonly IEnumerable<CoinsEntity> UpdatedCoins =
        [
            new CoinsEntity
            {
                Id = 1,
                Symbol = "BTC",
                Name = "Bitcoin",
                IsFiat = false,
                IdCoinGecko = "bitcoin",
                IsStablecoin = false,
                MarketCapUsd = int.MaxValue,
                PriceUsd = "100000",
                PriceChangePercentage24h = 100,
            },
            new CoinsEntity
            {
                Id = 2,
                Symbol = "USDT",
                Name = "Tether",
                IsFiat = false,
                IdCoinGecko = null,
                IsStablecoin = true,
                MarketCapUsd = int.MaxValue,
                PriceUsd = "1",
                PriceChangePercentage24h = 100,
            },
        ];
    }
}
