using AutoFixture;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SVC_Coins.Models.Entities;
using SVC_Coins.Models.Input;
using SVC_Coins.Repositories;

namespace SVC_Coins.Tests.Unit.Repositories;

public class CoinsRepositoryTests
{
    private readonly CoinsDbContext _context;
    private readonly CoinsRepository _repository;
    private readonly Fixture _fixture;

    public CoinsRepositoryTests()
    {
        _fixture = new Fixture();

        var options = new DbContextOptionsBuilder<CoinsDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        _context = new CoinsDbContext(options);
        _context.Database.OpenConnection();
        _context.Database.EnsureCreated();

        _repository = new CoinsRepository(_context);
    }

    [Fact]
    public async Task InsertCoin_AddsEntityToTheDatabase()
    {
        // Arrange
        var coinNew = _fixture.Build<CoinNew>().With(c => c.Symbol, "BTC").Create();
        var coinEntity = new CoinsEntity
        {
            Name = coinNew.Name,
            Symbol = coinNew.Symbol,
            IsStablecoin = coinNew.IsStablecoin,
            QuoteCoinPriority = coinNew.QuoteCoinPriority,
        };

        // Act
        var result = await _repository.InsertCoin(coinNew);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var insertedCoin = await _context.Coins.FirstOrDefaultAsync(e =>
            e.Symbol == coinNew.Symbol && e.Name == coinNew.Name
        );
        insertedCoin.Should().BeEquivalentTo(coinEntity, options => options.Excluding(c => c.Id));
    }

    [Fact]
    public async Task InsertCoin_ShouldReturnFail_IfCoinAlreadyExists()
    {
        // Arrange
        var coinNew = _fixture.Build<CoinNew>().With(c => c.Symbol, "BTC").Create();
        await _repository.InsertCoin(coinNew);

        // Act
        var result = await _repository.InsertCoin(coinNew);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.First().Message.Should().Be("Coin already exists in the database.");
    }

    [Fact]
    public async Task InsertCoins_ShouldAddMultipleCoinsToDatabase_WhenAllAreNew()
    {
        // Arrange
        var coinsNew = new List<CoinNew>
        {
            _fixture
                .Build<CoinNew>()
                .With(c => c.Name, "Bitcoin")
                .With(c => c.Symbol, "BTC")
                .Create(),
            _fixture
                .Build<CoinNew>()
                .With(c => c.Name, "Ethereum")
                .With(c => c.Symbol, "ETH")
                .Create(),
            _fixture
                .Build<CoinNew>()
                .With(c => c.Name, "Tether")
                .With(c => c.Symbol, "USDT")
                .Create(),
        };

        // Act
        var result = await _repository.InsertCoins(coinsNew);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var insertedCoins = await _context.Coins.ToListAsync();
        insertedCoins.Should().HaveCount(3);
        insertedCoins.Should().Contain(c => c.Symbol == "BTC" && c.Name == "Bitcoin");
        insertedCoins.Should().Contain(c => c.Symbol == "ETH" && c.Name == "Ethereum");
        insertedCoins.Should().Contain(c => c.Symbol == "USDT" && c.Name == "Tether");
    }

    [Fact]
    public async Task InsertCoins_ShouldReturnFail_WhenAnyCoinsExist()
    {
        // Arrange
        var existingCoin = new CoinsEntity { Name = "Bitcoin", Symbol = "BTC" };
        await _context.Coins.AddAsync(existingCoin);
        await _context.SaveChangesAsync();

        var coinsNew = new List<CoinNew>
        {
            _fixture
                .Build<CoinNew>()
                .With(c => c.Name, "Bitcoin")
                .With(c => c.Symbol, "BTC")
                .Create(),
            _fixture
                .Build<CoinNew>()
                .With(c => c.Name, "Ethereum")
                .With(c => c.Symbol, "ETH")
                .Create(),
        };

        // Act
        var result = await _repository.InsertCoins(coinsNew);

        // Assert
        result.IsFailed.Should().BeTrue();
        result
            .Errors.First()
            .Message.Should()
            .Be("The following coins already exist in the database: Bitcoin (BTC)");
        var coinsInDb = await _context.Coins.ToListAsync();
        coinsInDb.Should().HaveCount(1);
    }

    [Fact]
    public async Task InsertCoins_ShouldReturnFail_WhenMultipleCoinsExist()
    {
        // Arrange
        var existingCoins = new List<CoinsEntity>
        {
            new() { Name = "Bitcoin", Symbol = "BTC" },
            new() { Name = "Ethereum", Symbol = "ETH" },
        };
        await _context.Coins.AddRangeAsync(existingCoins);
        await _context.SaveChangesAsync();

        var coinsNew = new List<CoinNew>
        {
            _fixture
                .Build<CoinNew>()
                .With(c => c.Name, "Bitcoin")
                .With(c => c.Symbol, "BTC")
                .Create(),
            _fixture
                .Build<CoinNew>()
                .With(c => c.Name, "Ethereum")
                .With(c => c.Symbol, "ETH")
                .Create(),
            _fixture
                .Build<CoinNew>()
                .With(c => c.Name, "Tether")
                .With(c => c.Symbol, "USDT")
                .Create(),
        };

        // Act
        var result = await _repository.InsertCoins(coinsNew);

        // Assert
        result.IsFailed.Should().BeTrue();
        result
            .Errors.First()
            .Message.Should()
            .Be("The following coins already exist in the database: Bitcoin (BTC), Ethereum (ETH)");
        var coinsInDb = await _context.Coins.ToListAsync();
        coinsInDb.Should().HaveCount(2); // Only the original coins should exist
    }

    [Fact]
    public async Task GetCoins_ShouldReturnAllCoinsWithTradingPairs()
    {
        // Arrange
        var coin1 = new CoinsEntity
        {
            Id = 1,
            Name = "Bitcoin",
            Symbol = "BTC",
            TradingPairs = [],
        };
        var coin2 = new CoinsEntity
        {
            Id = 2,
            Name = "Ethereum",
            Symbol = "ETH",
            TradingPairs = [],
        };
        var coin3 = new CoinsEntity
        {
            Id = 3,
            Name = "Tether",
            Symbol = "USDT",
            TradingPairs = [],
        };

        await _context.Coins.AddRangeAsync([coin1, coin2, coin3]);
        await _context.SaveChangesAsync();

        var tradingPair1 = new TradingPairsEntity
        {
            Id = 1,
            IdCoinMain = coin1.Id,
            IdCoinQuote = coin3.Id,
            CoinMain = coin1,
            CoinQuote = coin3,
        };
        var tradingPair2 = new TradingPairsEntity
        {
            Id = 2,
            IdCoinMain = coin2.Id,
            IdCoinQuote = coin1.Id,
            CoinMain = coin2,
            CoinQuote = coin1,
        };

        coin1.TradingPairs.Add(tradingPair1);
        coin2.TradingPairs.Add(tradingPair2);

        await _context.TradingPairs.AddRangeAsync([tradingPair1, tradingPair2]);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllCoins();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);

        var firstCoin = result.First(c => c.Id == coin1.Id);
        firstCoin.TradingPairs.Should().HaveCount(1);

        var tradingPairCoinIds = firstCoin.TradingPairs.Select(tp => tp.CoinQuote.Id);
        tradingPairCoinIds.Should().Contain([coin3.Id]);
    }

    [Fact]
    public async Task DeleteCoin_ShouldRemoveCoinAndAssociatedTradingPairsFromDatabase()
    {
        // Arrange
        var coin1 = new CoinsEntity
        {
            Id = 1,
            Name = "Bitcoin",
            Symbol = "BTC",
        };
        var coin2 = new CoinsEntity
        {
            Id = 2,
            Name = "Ethereum",
            Symbol = "ETH",
        };
        var coin3 = new CoinsEntity
        {
            Id = 3,
            Name = "Tether",
            Symbol = "USDT",
        };

        await _context.Coins.AddRangeAsync(coin1, coin2, coin3);
        await _context.SaveChangesAsync();

        var coinToDeleteId = coin2.Id;

        var tradingPair1 = new TradingPairsEntity
        {
            Id = 1,
            IdCoinMain = coin2.Id,
            IdCoinQuote = coin1.Id,
            CoinMain = coin2,
            CoinQuote = coin1,
        };
        var tradingPair2 = new TradingPairsEntity
        {
            Id = 2,
            IdCoinMain = coin3.Id,
            IdCoinQuote = coin2.Id,
            CoinMain = coin3,
            CoinQuote = coin2,
        };
        var tradingPair3 = new TradingPairsEntity
        {
            Id = 3,
            IdCoinMain = coin1.Id,
            IdCoinQuote = coin3.Id,
            CoinMain = coin1,
            CoinQuote = coin3,
        };

        await _context.TradingPairs.AddRangeAsync(tradingPair1, tradingPair2, tradingPair3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.DeleteCoin(coinToDeleteId);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var deletedCoin = await _context.Coins.FirstOrDefaultAsync(c => c.Id == coinToDeleteId);
        deletedCoin.Should().BeNull();

        var remainingCoins = await _context.Coins.ToListAsync();
        remainingCoins.Should().HaveCount(2);
        remainingCoins.Should().NotContain(c => c.Id == coinToDeleteId);

        var remainingTradingPairs = await _context.TradingPairs.ToListAsync();
        remainingTradingPairs.Should().HaveCount(1);
        remainingTradingPairs
            .Should()
            .NotContain(tp => tp.IdCoinMain == coinToDeleteId || tp.IdCoinQuote == coinToDeleteId);
    }

    [Fact]
    public async Task DeleteCoin_ShouldReturnFail_IfCoinNotFound()
    {
        // Arrange
        var nonExistentId = 999;

        // Act
        var result = await _repository.DeleteCoin(nonExistentId);

        // Assert
        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task InsertTradingPair_ShouldAddTradingPairToDatabase_IfValid()
    {
        // Arrange
        var coinMain = new CoinsEntity { Name = "Bitcoin", Symbol = "BTC" };
        var coinQuote = new CoinsEntity { Name = "Ethereum", Symbol = "ETH" };
        await _context.Coins.AddRangeAsync(coinMain, coinQuote);
        await _context.SaveChangesAsync();

        var tradingPairNew = new TradingPairNew
        {
            IdCoinMain = coinMain.Id,
            IdCoinQuote = coinQuote.Id,
        };

        // Act
        var result = await _repository.InsertTradingPair(tradingPairNew);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var insertedId = result.Value;

        var exists = await _context.TradingPairs.AnyAsync(entity =>
            entity.IdCoinMain == tradingPairNew.IdCoinMain
            && entity.IdCoinQuote == tradingPairNew.IdCoinQuote
        );
        exists.Should().BeTrue();

        var insertedEntity = await _context.TradingPairs.FindAsync(insertedId);
        insertedEntity.Should().NotBeNull();
        insertedEntity!.Id.Should().Be(insertedId);
        insertedEntity.IdCoinMain.Should().Be(tradingPairNew.IdCoinMain);
        insertedEntity.IdCoinQuote.Should().Be(tradingPairNew.IdCoinQuote);
    }

    [Fact]
    public async Task InsertTradingPair_ShouldFail_IfCoinsDoNotExist()
    {
        // Arrange
        var tradingPairNew = new TradingPairNew
        {
            IdCoinMain = 999, // Non-existent
            IdCoinQuote = 1000, // Non-existent
        };

        // Act
        var result = await _repository.InsertTradingPair(tradingPairNew);

        // Assert
        result.IsFailed.Should().BeTrue();
        result
            .Errors.First()
            .Message.Should()
            .Be("One or both coins do not exist in the Coins table.");
    }

    [Fact]
    public async Task InsertTradingPair_ShouldFail_IfTradingPairAlreadyExists()
    {
        // Arrange
        var coinMain = new CoinsEntity { Name = "Bitcoin", Symbol = "BTC" };
        var coinQuote = new CoinsEntity { Name = "Ethereum", Symbol = "ETH" };
        await _context.Coins.AddRangeAsync(coinMain, coinQuote);
        await _context.SaveChangesAsync();

        var tradingPair = new TradingPairsEntity
        {
            IdCoinMain = coinMain.Id,
            IdCoinQuote = coinQuote.Id,
        };

        await _context.TradingPairs.AddAsync(tradingPair);
        await _context.SaveChangesAsync();

        var tradingPairNew = new TradingPairNew
        {
            IdCoinMain = coinMain.Id,
            IdCoinQuote = coinQuote.Id,
        };

        // Act
        var result = await _repository.InsertTradingPair(tradingPairNew);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.First().Message.Should().Be("This trading pair already exists.");
    }

    [Fact]
    public async Task GetQuoteCoinsPrioritized_ShouldReturnCoinsSortedByPriority()
    {
        // Arrange
        var coin1 = new CoinsEntity
        {
            Id = 1,
            Name = "Bitcoin",
            Symbol = "BTC",
            QuoteCoinPriority = 2,
        };
        var coin2 = new CoinsEntity
        {
            Id = 2,
            Name = "Ethereum",
            Symbol = "ETH",
            QuoteCoinPriority = 1,
        };
        var coin3 = new CoinsEntity
        {
            Id = 3,
            Name = "Tether",
            Symbol = "USDT",
            QuoteCoinPriority = null,
        };

        await _context.Coins.AddRangeAsync(coin1, coin2, coin3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetQuoteCoinsPrioritized();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);

        var orderedCoins = result.ToList();
        orderedCoins[0].Id.Should().Be(coin2.Id);
        orderedCoins[1].Id.Should().Be(coin1.Id);
    }

    [Fact]
    public async Task GetQuoteCoinsPrioritized_ShouldReturnEmptyIfNoCoinsExist()
    {
        // Act
        var result = await _repository.GetQuoteCoinsPrioritized();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCoinsByIds_ShouldReturnCorrectCoins()
    {
        // Arrange: Use a new context for setup
        var options = new DbContextOptionsBuilder<CoinsDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase")
            .Options;

        using var setupContext = new CoinsDbContext(options);
        var coin1 = new CoinsEntity
        {
            Id = 1,
            Name = "Bitcoin",
            Symbol = "BTC",
            TradingPairs =
            [
                new TradingPairsEntity
                {
                    Id = 1,
                    IdCoinMain = 1,
                    IdCoinQuote = 2,
                },
            ],
        };
        var coin2 = new CoinsEntity
        {
            Id = 2,
            Name = "Ethereum",
            Symbol = "ETH",
            TradingPairs = [],
        };
        await setupContext.Coins.AddRangeAsync(coin1, coin2);
        await setupContext.SaveChangesAsync();

        // Act: Use a separate context for the test
        using var testContext = new CoinsDbContext(options);
        var repository = new CoinsRepository(testContext);
        var result = await repository.GetCoinsByIds([1]);

        // Assert: Validate results using the test context
        result.Should().HaveCount(1);
        result.Should().Contain(c => c.Id == 1 && c.Name == "Bitcoin");
        result.First().TradingPairs.Should().HaveCount(1);
        result.First().TradingPairs.First().CoinQuote.Id.Should().Be(2);
    }

    [Fact]
    public async Task GetCoinsByIds_ShouldReturnEmpty_IfNoCoinsExist()
    {
        // Act
        var result = await _repository.GetCoinsByIds([1, 2]);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ResetDatabase_ShouldRemoveAllCoinsAndTradingPairs()
    {
        // Arrange
        var coin1 = new CoinsEntity { Name = "Bitcoin", Symbol = "BTC" };
        var coin2 = new CoinsEntity { Name = "Ethereum", Symbol = "ETH" };
        var coin3 = new CoinsEntity { Name = "Tether", Symbol = "USDT" };

        await _context.Coins.AddRangeAsync([coin1, coin2, coin3]);
        await _context.SaveChangesAsync();

        var tradingPair1 = new TradingPairsEntity
        {
            IdCoinMain = coin1.Id,
            IdCoinQuote = coin2.Id,
            CoinMain = coin1,
            CoinQuote = coin2,
        };
        var tradingPair2 = new TradingPairsEntity
        {
            IdCoinMain = coin2.Id,
            IdCoinQuote = coin3.Id,
            CoinMain = coin2,
            CoinQuote = coin3,
        };

        await _context.TradingPairs.AddRangeAsync([tradingPair1, tradingPair2]);
        await _context.SaveChangesAsync();

        // Verify initial state
        (await _context.Coins.CountAsync())
            .Should()
            .Be(3);
        (await _context.TradingPairs.CountAsync()).Should().Be(2);

        // Act
        await _repository.ResetDatabase();

        // Assert
        (await _context.Coins.CountAsync())
            .Should()
            .Be(0);
        (await _context.TradingPairs.CountAsync()).Should().Be(0);
    }
}
