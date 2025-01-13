using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SVC_Kline.Models.Entities;
using SVC_Kline.Models.Input;
using SVC_Kline.Repositories;

namespace SVC_Kline.Tests.Unit.Repositories;

public class KlineDataRepositoryTests
{
    private readonly IFixture _fixture;
    private readonly KlineDataDbContext _context;
    private readonly KlineDataRepository _repository;

    public KlineDataRepositoryTests()
    {
        _fixture = new Fixture().Customize(new AutoMoqCustomization());

        var options = new DbContextOptionsBuilder<KlineDataDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        _context = new KlineDataDbContext(options);
        _context.Database.OpenConnection();
        _context.Database.EnsureCreated();

        _repository = new KlineDataRepository(_context);
    }

    private async Task<TradingPairEntity> CreateTradingPairAsync()
    {
        var tradingPair = new TradingPairEntity();
        _context.TradingPair.Add(tradingPair);
        await _context.SaveChangesAsync();
        return tradingPair;
    }

    [Fact]
    public async Task InsertKlineData_ShouldAddEntityToDatabase()
    {
        // Arrange
        var tradingPair = await CreateTradingPairAsync();
        var klineData = _fixture
            .Build<KlineDataNew>()
            .With(x => x.IdTradePair, tradingPair.Id)
            .Create();

        // Act
        await _repository.InsertKlineData(klineData);

        // Assert
        var entity = await _context.KlineData.FirstOrDefaultAsync(e =>
            e.IdTradePair == klineData.IdTradePair
        );
        entity.Should().NotBeNull();
        entity!.OpenPrice.Should().Be(klineData.OpenPrice);
    }

    [Fact]
    public async Task InsertManyKlineData_ShouldAddEntitiesToDatabase()
    {
        // Arrange
        var tradingPair = await CreateTradingPairAsync();
        var klineDataList = _fixture
            .Build<KlineDataNew>()
            .With(x => x.IdTradePair, tradingPair.Id)
            .CreateMany(5)
            .ToList();

        // Act
        await _repository.InsertManyKlineData(klineDataList);

        // Assert
        var entities = await _context.KlineData.ToListAsync();
        entities.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetAllKlineData_ShouldReturnAllEntitiesGroupedByTradingPairId()
    {
        // Arrange
        var tradingPair1 = await CreateTradingPairAsync();
        var tradingPair2 = await CreateTradingPairAsync();

        var klineDataEntities = new List<KlineDataEntity>
        {
            _fixture
                .Build<KlineDataEntity>()
                .With(x => x.IdTradePair, tradingPair1.Id)
                .Without(x => x.IdTradePairNavigation)
                .Create(),
            _fixture
                .Build<KlineDataEntity>()
                .With(x => x.IdTradePair, tradingPair1.Id)
                .Without(x => x.IdTradePairNavigation)
                .Create(),
            _fixture
                .Build<KlineDataEntity>()
                .With(x => x.IdTradePair, tradingPair2.Id)
                .Without(x => x.IdTradePairNavigation)
                .Create(),
        };
        await _context.KlineData.AddRangeAsync(klineDataEntities);
        await _context.SaveChangesAsync();

        // Act
        var klineDataDict = await _repository.GetAllKlineData();

        // Assert
        klineDataDict.Should().HaveCount(2);
        klineDataDict[tradingPair1.Id].Should().HaveCount(2);
        klineDataDict[tradingPair2.Id].Should().HaveCount(1);
    }

    [Fact]
    public async Task DeleteKlineDataForTradingPair_ShouldRemoveEntities()
    {
        // Arrange
        var tradingPair1 = await CreateTradingPairAsync();
        var tradingPair2 = await CreateTradingPairAsync();

        var klineDataEntities = new List<KlineDataEntity>
        {
            _fixture
                .Build<KlineDataEntity>()
                .With(x => x.IdTradePair, tradingPair1.Id)
                .Without(x => x.IdTradePairNavigation)
                .Create(),
            _fixture
                .Build<KlineDataEntity>()
                .With(x => x.IdTradePair, tradingPair1.Id)
                .Without(x => x.IdTradePairNavigation)
                .Create(),
            _fixture
                .Build<KlineDataEntity>()
                .With(x => x.IdTradePair, tradingPair2.Id)
                .Without(x => x.IdTradePairNavigation)
                .Create(),
        };
        await _context.KlineData.AddRangeAsync(klineDataEntities);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteKlineDataForTradingPair(tradingPair1.Id);

        // Assert
        var remainingEntities = await _context.KlineData.ToListAsync();
        remainingEntities.Should().ContainSingle();
        remainingEntities[0].IdTradePair.Should().Be(tradingPair2.Id);
    }

    [Fact]
    public async Task ReplaceAllKlineData_ShouldReplaceExistingData()
    {
        // Arrange
        var tradingPair1 = await CreateTradingPairAsync();
        var tradingPair2 = await CreateTradingPairAsync();

        var existingData = new List<KlineDataEntity>
        {
            _fixture
                .Build<KlineDataEntity>()
                .With(x => x.IdTradePair, tradingPair1.Id)
                .Without(x => x.IdTradePairNavigation)
                .Create(),
            _fixture
                .Build<KlineDataEntity>()
                .With(x => x.IdTradePair, tradingPair1.Id)
                .Without(x => x.IdTradePairNavigation)
                .Create(),
            _fixture
                .Build<KlineDataEntity>()
                .With(x => x.IdTradePair, tradingPair2.Id)
                .Without(x => x.IdTradePairNavigation)
                .Create(),
        };
        await _context.KlineData.AddRangeAsync(existingData);
        await _context.SaveChangesAsync();

        var newTradingPair = await CreateTradingPairAsync();
        var newKlineData = _fixture
            .Build<KlineDataNew>()
            .With(x => x.IdTradePair, newTradingPair.Id)
            .CreateMany(3)
            .ToArray();

        // Act
        await _repository.ReplaceAllKlineData(newKlineData);

        // Assert
        var entities = await _context.KlineData.ToListAsync();
        entities.Should().HaveCount(3);
        entities.Should().AllSatisfy(e => e.IdTradePair.Should().Be(newTradingPair.Id));
    }
}
