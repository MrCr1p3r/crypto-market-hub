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

    public CoinsRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<CoinsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new CoinsDbContext(options);
        _repository = new CoinsRepository(_context);
    }

    [Fact]
    public async Task InsertCoin_AddsEntityToTheDatabase()
    {
        // Arrange
        var coinNew = new CoinNew { Name = "Bitcoin", Symbol = "BTC" };

        // Act
        var result = await _repository.InsertCoin(coinNew);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var exists = await _context.Coins.AnyAsync(e =>
            e.Symbol == coinNew.Symbol && e.Name == coinNew.Name
        );
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task InsertCoin_ShouldReturnFail_IfCoinAlreadyExists()
    {
        // Arrange
        var coinNew = new CoinNew { Name = "Bitcoin", Symbol = "BTC" };
        await _repository.InsertCoin(coinNew);

        // Act
        var result = await _repository.InsertCoin(coinNew);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.First().Message.Should().Be("Coin already exists in the database.");
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
}
