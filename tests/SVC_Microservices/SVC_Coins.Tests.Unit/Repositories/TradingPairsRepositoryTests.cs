using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SVC_Coins.Domain.Entities;
using SVC_Coins.Domain.ValueObjects;
using SVC_Coins.Infrastructure;
using SVC_Coins.Repositories;

namespace SVC_Coins.Tests.Unit.Repositories;

public class TradingPairsRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly CoinsDbContext _seedContext;
    private readonly CoinsDbContext _actContext;
    private readonly CoinsDbContext _assertContext;
    private readonly TradingPairsRepository _testedRepository;

    public TradingPairsRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<CoinsDbContext>().UseSqlite(_connection).Options;

        _seedContext = new CoinsDbContext(options);
        _actContext = new CoinsDbContext(options);
        _assertContext = new CoinsDbContext(options);
        _seedContext.Database.EnsureCreated();

        _testedRepository = new TradingPairsRepository(_actContext);
    }

    [Fact]
    public async Task GetTradingPairsByCoinIdsPairs_ReturnsTradingPairs()
    {
        // Arrange
        await SeedDatabase(addTradingPairs: true);

        // Act
        var result = await _testedRepository.GetTradingPairsByCoinIdPairs(
            TestData.TradingPairsCoinIdsPairs
        );

        // Assert
        result
            .Should()
            .BeEquivalentTo(TestData.FoundTradingPairs, options => options.Excluding(tp => tp.Id));
    }

    [Fact]
    public async Task GetTradingPairsByCoinIdPairs_WhenNoPairsFound_ReturnsEmptyCollection()
    {
        // Act
        var result = await _testedRepository.GetTradingPairsByCoinIdPairs(
            TestData.TradingPairsCoinIdsPairs
        );

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTradingPairsByCoinIdPairs_WhenNoPairsRequested_ReturnsEmptyCollection()
    {
        // Act
        var result = await _testedRepository.GetTradingPairsByCoinIdPairs([]);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task InsertTradingPairs_InsertsPairsIntoDatabase()
    {
        // Arrange
        await SeedDatabase();
        var exchanges = await _actContext.Exchanges.ToListAsync();
        var tradingPairs = TestData.GetTradingPairs(exchanges);

        // Act
        await _testedRepository.InsertTradingPairs(tradingPairs);

        // Assert
        var tradingPairsInDb = await _assertContext
            .TradingPairs.Include(tp => tp.Exchanges)
            .ToListAsync();
        tradingPairsInDb
            .Should()
            .BeEquivalentTo(
                TestData.InsertedTradingPairs,
                options =>
                    options
                        .Excluding(tp => tp.Id)
                        .For(tp => tp.Exchanges)
                        .Exclude(exchange => exchange.Id)
            );
    }

    [Fact]
    public async Task InsertTradingPairs_ReturnsInsertedPairs()
    {
        // Arrange
        await SeedDatabase();
        var exchanges = await _actContext.Exchanges.ToListAsync();
        var tradingPairs = TestData.GetTradingPairs(exchanges);

        // Act
        var result = await _testedRepository.InsertTradingPairs(tradingPairs);

        // Assert
        result.First().Id.Should().Be(1);
        result.Last().Id.Should().Be(2);
        result
            .Should()
            .BeEquivalentTo(
                TestData.InsertedTradingPairs,
                options =>
                    options
                        .Excluding(tp => tp.Id)
                        .For(tp => tp.Exchanges)
                        .Exclude(exchange => exchange.Id)
            );
    }

    [Fact]
    public async Task InsertTradingPairs_WhenCollectionIsEmpty_ShouldDoNothing()
    {
        // Act
        var result = await _testedRepository.InsertTradingPairs([]);

        // Assert
        var tradingPairsInDb = await _assertContext.TradingPairs.ToListAsync();
        tradingPairsInDb.Should().BeEmpty();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ReplaceAllTradingPairs_ReplacesAllPairsInDatabase()
    {
        // Arrange
        await SeedDatabase(addTradingPairs: true);
        var exchanges = await _actContext.Exchanges.ToListAsync();
        var tradingPairs = TestData.GetNewTradingPairs(exchanges);

        // Act
        await _testedRepository.ReplaceAllTradingPairs(tradingPairs);

        // Assert
        var tradingPairsInDb = await _assertContext
            .TradingPairs.Include(tp => tp.Exchanges)
            .ToListAsync();
        tradingPairsInDb.Should().HaveCount(TestData.NewInsertedTradingPairs.Count());
        tradingPairsInDb
            .Should()
            .BeEquivalentTo(
                TestData.NewInsertedTradingPairs,
                options =>
                    options
                        .Excluding(tp => tp.Id)
                        .For(tp => tp.Exchanges)
                        .Exclude(exchange => exchange.Id)
            );
    }

    [Fact]
    public async Task ReplaceAllTradingPairs_ReturnsNewPairs()
    {
        // Arrange
        await SeedDatabase(addTradingPairs: true);
        var exchanges = await _actContext.Exchanges.ToListAsync();
        var tradingPairs = TestData.GetNewTradingPairs(exchanges);

        // Act
        var result = await _testedRepository.ReplaceAllTradingPairs(tradingPairs);

        // Assert
        result
            .Should()
            .BeEquivalentTo(
                TestData.NewInsertedTradingPairs,
                options =>
                    options
                        .Excluding(tp => tp.Id)
                        .For(tp => tp.Exchanges)
                        .Exclude(exchange => exchange.Id)
            );
    }

    [Fact]
    public async Task DeleteTradingPairsForIdCoin_DeletesPairsForCoin()
    {
        // Arrange
        await SeedDatabase(addTradingPairs: true);

        // Act
        await _testedRepository.DeleteTradingPairsForIdCoin(1);

        // Assert
        var tradingPairsInDb = await _actContext.TradingPairs.ToListAsync();
        tradingPairsInDb.Should().BeEmpty();
    }

    private async Task SeedDatabase(bool addTradingPairs = false)
    {
        await _seedContext.Coins.AddRangeAsync(TestData.GetCoins());
        var exchanges = TestData.GetExchanges().ToList();
        await _seedContext.Exchanges.AddRangeAsync(exchanges);
        if (addTradingPairs)
        {
            var tradingPairs = TestData.GetTradingPairs(exchanges);
            await _seedContext.TradingPairs.AddRangeAsync(tradingPairs);
        }

        await _seedContext.SaveChangesAsync();
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _seedContext.Dispose();
            _actContext.Dispose();
            _assertContext.Dispose();
            _connection.Dispose();
        }
    }

    private static class TestData
    {
        public static IEnumerable<CoinsEntity> GetCoins() =>
            [
                new CoinsEntity { Name = "Bitcoin", Symbol = "BTC" },
                new CoinsEntity { Name = "Ethereum", Symbol = "ETH" },
                new CoinsEntity { Name = "Tether", Symbol = "USDT" },
            ];

        public static IEnumerable<ExchangesEntity> GetExchanges() =>
            [new ExchangesEntity { Name = "Binance" }, new ExchangesEntity { Name = "Kraken" }];

        public static IEnumerable<TradingPairsEntity> GetTradingPairs(
            List<ExchangesEntity> exchanges
        ) =>
            [
                new TradingPairsEntity
                {
                    IdCoinMain = 1,
                    IdCoinQuote = 2,
                    Exchanges = exchanges,
                },
                new TradingPairsEntity
                {
                    IdCoinMain = 2,
                    IdCoinQuote = 1,
                    Exchanges = [exchanges[0]],
                },
            ];

        public static IEnumerable<TradingPairCoinIdsPair> TradingPairsCoinIdsPairs =>
            [
                new TradingPairCoinIdsPair { IdCoinMain = 1, IdCoinQuote = 2 },
                new TradingPairCoinIdsPair { IdCoinMain = 2, IdCoinQuote = 1 },
            ];

        public static readonly IEnumerable<TradingPairsEntity> FoundTradingPairs =
        [
            new TradingPairsEntity { IdCoinMain = 1, IdCoinQuote = 2 },
            new TradingPairsEntity { IdCoinMain = 2, IdCoinQuote = 1 },
        ];

        public static readonly IEnumerable<TradingPairsEntity> InsertedTradingPairs =
        [
            new TradingPairsEntity
            {
                IdCoinMain = 1,
                IdCoinQuote = 2,
                Exchanges =
                [
                    new ExchangesEntity { Name = "Binance" },
                    new ExchangesEntity { Name = "Kraken" },
                ],
            },
            new TradingPairsEntity
            {
                IdCoinMain = 2,
                IdCoinQuote = 1,
                Exchanges = [new ExchangesEntity { Name = "Binance" }],
            },
        ];

        public static IEnumerable<TradingPairsEntity> GetNewTradingPairs(
            List<ExchangesEntity> exchanges
        ) =>
            [
                new TradingPairsEntity
                {
                    IdCoinMain = 1,
                    IdCoinQuote = 3,
                    Exchanges = exchanges,
                },
                new TradingPairsEntity
                {
                    IdCoinMain = 2,
                    IdCoinQuote = 3,
                    Exchanges = [exchanges[0]],
                },
            ];

        public static readonly IEnumerable<TradingPairsEntity> NewInsertedTradingPairs =
        [
            new TradingPairsEntity
            {
                IdCoinMain = 1,
                IdCoinQuote = 3,
                Exchanges =
                [
                    new ExchangesEntity { Name = "Binance" },
                    new ExchangesEntity { Name = "Kraken" },
                ],
            },
            new TradingPairsEntity
            {
                IdCoinMain = 2,
                IdCoinQuote = 3,
                Exchanges = [new ExchangesEntity { Name = "Binance" }],
            },
        ];
    }
}
