using AutoFixture;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SVC_Coins.Models.Entities;
using SVC_Coins.Models.Input;
using SVC_Coins.Models.Output;
using SVC_Coins.Repositories;

namespace SVC_Coins.Tests.Unit.Repositories;

public class CoinsRepositoryTests
{
    private readonly IFixture _fixture;
    private readonly CoinsDbContext _context;
    private readonly CoinsRepository _repository;

    public CoinsRepositoryTests()
    {
        _fixture = new Fixture();
        _fixture.Behaviors.Remove(new ThrowingRecursionBehavior());
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        var options = new DbContextOptionsBuilder<CoinsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new CoinsDbContext(options);

        _repository = new CoinsRepository(_context);
    }

    [Fact]
    public async Task InsertCoin_ShouldAddEntityToDatabase()
    {
        // Arrange
        var coinNew = _fixture.Create<CoinNew>();

        // Act
        await _repository.InsertCoin(coinNew);

        // Assert
        var exists = await _context.Coins.AnyAsync(e =>
            e.Symbol == coinNew.Symbol && e.Name == coinNew.Name
        );
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task GetCoins_ShouldReturnAllCoinsWithTradingPairs()
    {
        // Arrange
        var coin1 = new CoinEntity
        {
            Id = 1,
            Name = "Bitcoin",
            Symbol = "BTC",
            TradingPairs = [],
        };
        var coin2 = new CoinEntity
        {
            Id = 2,
            Name = "Ethereum",
            Symbol = "ETH",
            TradingPairs = [],
        };
        var coin3 = new CoinEntity
        {
            Id = 3,
            Name = "Tether",
            Symbol = "USDT",
            TradingPairs = [],
        };

        await _context.Coins.AddRangeAsync([coin1, coin2, coin3]);
        await _context.SaveChangesAsync();

        var tradingPair1 = new TradingPairEntity
        {
            Id = 1,
            IdCoinMain = coin1.Id,
            IdCoinQuote = coin3.Id,
            CoinMain = coin1,
            CoinQuote = coin3,
        };
        var tradingPair2 = new TradingPairEntity
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
        var coin1 = new CoinEntity
        {
            Id = 1,
            Name = "Bitcoin",
            Symbol = "BTC",
        };
        var coin2 = new CoinEntity
        {
            Id = 2,
            Name = "Ethereum",
            Symbol = "ETH",
        };
        var coin3 = new CoinEntity
        {
            Id = 3,
            Name = "Tether",
            Symbol = "USDT",
        };

        await _context.Coins.AddRangeAsync([coin1, coin2, coin3]);
        await _context.SaveChangesAsync();

        var coinToDelete = coin2;
        var coinToDeleteId = coinToDelete.Id;

        var tradingPair1 = new TradingPairEntity
        {
            Id = 1,
            IdCoinMain = coinToDelete.Id,
            IdCoinQuote = coin1.Id,
            CoinMain = coinToDelete,
            CoinQuote = coin1,
        };
        var tradingPair2 = new TradingPairEntity
        {
            Id = 2,
            IdCoinMain = coin3.Id,
            IdCoinQuote = coinToDelete.Id,
            CoinMain = coin3,
            CoinQuote = coinToDelete,
        };
        var tradingPair3 = new TradingPairEntity
        {
            Id = 3,
            IdCoinMain = coin1.Id,
            IdCoinQuote = coin3.Id,
            CoinMain = coin1,
            CoinQuote = coin3,
        };

        await _context.TradingPairs.AddRangeAsync([tradingPair1, tradingPair2, tradingPair3]);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteCoin(coinToDeleteId);

        // Assert
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
    public async Task InsertTradingPair_ShouldAddTradingPairToDatabase()
    {
        // Arrange
        var tradingPairNew = _fixture.Create<TradingPairNew>();

        //Act
        await _repository.InsertTradingPair(tradingPairNew);

        //Assert
        var exists = await _context.TradingPairs.AnyAsync(entity =>
            entity.IdCoinMain == tradingPairNew.IdCoinMain
            && entity.IdCoinQuote == tradingPairNew.IdCoinQuote
        );
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task InsertTradingPair_ShouldReturnInsertedId()
    {
        // Arrange
        var coinMain = new CoinEntity { Name = "Bitcoin", Symbol = "BTC" };
        var coinQuote = new CoinEntity { Name = "Ethereum", Symbol = "ETH" };

        await _context.Coins.AddRangeAsync(coinMain, coinQuote);
        await _context.SaveChangesAsync();

        var tradingPairNew = new TradingPairNew
        {
            IdCoinMain = coinMain.Id,
            IdCoinQuote = coinQuote.Id,
        };

        // Act
        var insertedId = await _repository.InsertTradingPair(tradingPairNew);

        // Assert
        var insertedEntity = await _context.TradingPairs.FindAsync(insertedId);
        insertedEntity.Should().NotBeNull();
        insertedEntity!.Id.Should().Be(insertedId);
        insertedEntity.IdCoinMain.Should().Be(tradingPairNew.IdCoinMain);
        insertedEntity.IdCoinQuote.Should().Be(tradingPairNew.IdCoinQuote);
    }

    [Fact]
    public async Task GetQuoteCoinsPrioritized_ShouldReturnCoinsSortedByPriority()
    {
        // Arrange
        var coin1 = new CoinEntity
        {
            Id = 1,
            Name = "Bitcoin",
            Symbol = "BTC",
            QuoteCoinPriority = 2,
        };
        var coin2 = new CoinEntity
        {
            Id = 2,
            Name = "Ethereum",
            Symbol = "ETH",
            QuoteCoinPriority = 1,
        };
        var coin3 = new CoinEntity
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
