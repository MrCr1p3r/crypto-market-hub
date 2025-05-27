using Microsoft.EntityFrameworkCore;
using SVC_Kline.ApiContracts.Requests;
using SVC_Kline.Domain.Entities;
using SVC_Kline.Infrastructure;
using SVC_Kline.Repositories;

namespace SVC_Kline.Tests.Unit.Repositories;

public class KlineDataRepositoryTests : IDisposable
{
    private readonly Fixture _fixture;
    private readonly KlineDataDbContext _context;
    private readonly KlineDataRepository _repository;

    public KlineDataRepositoryTests()
    {
        _fixture = new Fixture();

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
    public async Task GetAllKlineData_ShouldReturnAllEntitiesGroupedByTradingPairId()
    {
        // Arrange
        var tradingPair1 = await CreateTradingPairAsync();
        var tradingPair2 = await CreateTradingPairAsync();

        var klineDataEntities = new List<KlineDataEntity>
        {
            _fixture
                .Build<KlineDataEntity>()
                .With(x => x.IdTradingPair, tradingPair1.Id)
                .With(x => x.OpenPrice, _fixture.Create<decimal>().ToString())
                .With(x => x.HighPrice, _fixture.Create<decimal>().ToString())
                .With(x => x.LowPrice, _fixture.Create<decimal>().ToString())
                .With(x => x.ClosePrice, _fixture.Create<decimal>().ToString())
                .With(x => x.Volume, _fixture.Create<decimal>().ToString())
                .Without(x => x.IdTradePairNavigation)
                .Create(),
            _fixture
                .Build<KlineDataEntity>()
                .With(x => x.IdTradingPair, tradingPair1.Id)
                .With(x => x.OpenPrice, _fixture.Create<decimal>().ToString())
                .With(x => x.HighPrice, _fixture.Create<decimal>().ToString())
                .With(x => x.LowPrice, _fixture.Create<decimal>().ToString())
                .With(x => x.ClosePrice, _fixture.Create<decimal>().ToString())
                .With(x => x.Volume, _fixture.Create<decimal>().ToString())
                .Without(x => x.IdTradePairNavigation)
                .Create(),
            _fixture
                .Build<KlineDataEntity>()
                .With(x => x.IdTradingPair, tradingPair2.Id)
                .With(x => x.OpenPrice, _fixture.Create<decimal>().ToString())
                .With(x => x.HighPrice, _fixture.Create<decimal>().ToString())
                .With(x => x.LowPrice, _fixture.Create<decimal>().ToString())
                .With(x => x.ClosePrice, _fixture.Create<decimal>().ToString())
                .With(x => x.Volume, _fixture.Create<decimal>().ToString())
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
    public async Task InsertManyKlineData_ShouldAddEntitiesToDatabase()
    {
        // Arrange
        var tradingPair = await CreateTradingPairAsync();
        var klineDataList = _fixture
            .Build<KlineDataCreationRequest>()
            .With(x => x.IdTradingPair, tradingPair.Id)
            .CreateMany(5)
            .ToList();

        // Act
        await _repository.InsertKlineData(klineDataList);

        // Assert
        var entities = await _context.KlineData.ToListAsync();
        entities.Should().HaveCount(5);
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
                .With(x => x.IdTradingPair, tradingPair1.Id)
                .Without(x => x.IdTradePairNavigation)
                .Create(),
            _fixture
                .Build<KlineDataEntity>()
                .With(x => x.IdTradingPair, tradingPair1.Id)
                .Without(x => x.IdTradePairNavigation)
                .Create(),
            _fixture
                .Build<KlineDataEntity>()
                .With(x => x.IdTradingPair, tradingPair2.Id)
                .Without(x => x.IdTradePairNavigation)
                .Create(),
        };
        await _context.KlineData.AddRangeAsync(existingData);
        await _context.SaveChangesAsync();

        var newTradingPair = await CreateTradingPairAsync();
        var newKlineData = _fixture
            .Build<KlineDataCreationRequest>()
            .With(x => x.IdTradingPair, newTradingPair.Id)
            .CreateMany(3)
            .ToArray();

        // Act
        await _repository.ReplaceAllKlineData(newKlineData);

        // Assert
        var entities = await _context.KlineData.ToListAsync();
        entities.Should().HaveCount(3);
        entities.Should().AllSatisfy(e => e.IdTradingPair.Should().Be(newTradingPair.Id));
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
}
