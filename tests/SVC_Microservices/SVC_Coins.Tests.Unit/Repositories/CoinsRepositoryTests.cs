using AutoFixture;
using AutoFixture.AutoMoq;
using AutoMapper;
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
    private readonly IMapper _mapper;
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

        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<CoinNew, CoinEntity>().ReverseMap();
            cfg.CreateMap<CoinEntity, Coin>()
                .ForMember(dest => dest.TradingPairs, opt => opt.MapFrom(src => src.TradingPairs));
            // Mapping from TradingPairEntity to TradingPair (output model)
            cfg.CreateMap<TradingPairEntity, TradingPair>()
                .ForMember(dest => dest.CoinQuote, opt => opt.MapFrom(src => src.CoinQuote));
            // Mapping from CoinEntity to Coin (for CoinQuote in TradingPair)
            cfg.CreateMap<CoinEntity, Coin>();
        });
        _mapper = config.CreateMapper();

        _repository = new CoinsRepository(_context, _mapper);
    }

    [Fact]
    public async Task InsertCoin_ShouldAddEntityToDatabase()
    {
        // Arrange
        var coinNew = _fixture.Create<CoinNew>();
        var coinEntity = _mapper.Map<CoinEntity>(coinNew);

        // Act
        await _repository.InsertCoin(coinNew);

        // Assert
        var entity = await _context.Coins.FirstOrDefaultAsync(e => e.Symbol == coinNew.Symbol && 
                                                                    e.Name == coinNew.Name);
        entity.Should().NotBeNull();
        entity!.Name.Should().Be(coinEntity.Name);
        entity.Symbol.Should().Be(coinEntity.Symbol);
    }

    [Fact]
    public async Task GetCoins_ShouldReturnAllCoinsWithTradingPairs()
    {
        // Arrange
        // Manually create coins
        var coin1 = new CoinEntity { Id = 1, Name = "Bitcoin", Symbol = "BTC", TradingPairs = new List<TradingPairEntity>() };
        var coin2 = new CoinEntity { Id = 2, Name = "Ethereum", Symbol = "ETH", TradingPairs = new List<TradingPairEntity>() };
        var coin3 = new CoinEntity { Id = 3, Name = "Tether", Symbol = "USDT", TradingPairs = new List<TradingPairEntity>() };

        await _context.Coins.AddRangeAsync(new[] { coin1, coin2, coin3 });
        await _context.SaveChangesAsync();

        // Create and add trading pairs for coins
        var tradingPair1 = new TradingPairEntity
        {
            Id = 1,
            IdCoinMain = coin1.Id,
            IdCoinQuote = coin2.Id,
            CoinMain = coin1,
            CoinQuote = coin2
        };
        var tradingPair2 = new TradingPairEntity
        {
            Id = 2,
            IdCoinMain = coin1.Id,
            IdCoinQuote = coin3.Id,
            CoinMain = coin1,
            CoinQuote = coin3
        };

        await _context.TradingPairs.AddRangeAsync(new[] { tradingPair1, tradingPair2 });
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllCoins();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);

        // Verify the first coin and its trading pairs
        var firstCoin = result.First(c => c.Id == coin1.Id);
        firstCoin.TradingPairs.Should().HaveCount(2);

        var tradingPairCoinIds = firstCoin.TradingPairs.Select(tp => tp.CoinQuote.Id).ToList();
        tradingPairCoinIds.Should().Contain(new[] { coin2.Id, coin3.Id });
    }

    [Fact]
    public async Task DeleteCoin_ShouldRemoveCoinAndAssociatedTradingPairsFromDatabase()
    {
        // Arrange
        // Manually create coins
        var coin1 = new CoinEntity { Id = 1, Name = "Bitcoin", Symbol = "BTC", TradingPairs = new List<TradingPairEntity>() };
        var coin2 = new CoinEntity { Id = 2, Name = "Ethereum", Symbol = "ETH", TradingPairs = new List<TradingPairEntity>() };
        var coin3 = new CoinEntity { Id = 3, Name = "Tether", Symbol = "USDT", TradingPairs = new List<TradingPairEntity>() };

        await _context.Coins.AddRangeAsync([coin1, coin2, coin3]);
        await _context.SaveChangesAsync();

        var coinToDelete = coin2;
        var coinToDeleteId = coinToDelete.Id;

        // Create TradingPairs involving the coin to delete
        var tradingPair1 = new TradingPairEntity
        {
            Id = 1,
            IdCoinMain = coinToDelete.Id,
            IdCoinQuote = coin1.Id,
            CoinMain = coinToDelete,
            CoinQuote = coin1
        };
        var tradingPair2 = new TradingPairEntity
        {
            Id = 2,
            IdCoinMain = coin3.Id,
            IdCoinQuote = coinToDelete.Id,
            CoinMain = coin3,
            CoinQuote = coinToDelete
        };
        // TradingPair not involving coinToDelete
        var tradingPair3 = new TradingPairEntity
        {
            Id = 3,
            IdCoinMain = coin1.Id,
            IdCoinQuote = coin3.Id,
            CoinMain = coin1,
            CoinQuote = coin3
        };

        await _context.TradingPairs.AddRangeAsync(new[] { tradingPair1, tradingPair2, tradingPair3 });
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteCoin(coinToDeleteId);

        // Assert
        // Verify the coin has been deleted
        var deletedCoin = await _context.Coins.FirstOrDefaultAsync(c => c.Id == coinToDeleteId);
        deletedCoin.Should().BeNull();

        var remainingCoins = await _context.Coins.ToListAsync();
        remainingCoins.Should().HaveCount(2);
        remainingCoins.Should().NotContain(c => c.Id == coinToDeleteId);

        // Verify that associated trading pairs have been deleted
        var remainingTradingPairs = await _context.TradingPairs.ToListAsync();
        remainingTradingPairs.Should().HaveCount(1); // Only the trading pair not involving coinToDelete should remain
        remainingTradingPairs.Should().NotContain(tp => tp.IdCoinMain == coinToDeleteId || tp.IdCoinQuote == coinToDeleteId);
    }
}
