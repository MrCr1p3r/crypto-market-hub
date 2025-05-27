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
    public async Task GetAllKlineData_ShouldReturnKlineDataResponsesGroupedByTradingPairId()
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
        var klineDataResponses = await _repository.GetAllKlineData();

        // Assert
        var responseList = klineDataResponses.ToList();
        responseList.Should().HaveCount(2);

        var tradingPair1Response = responseList.First(r => r.IdTradingPair == tradingPair1.Id);
        tradingPair1Response.KlineData.Should().HaveCount(2);

        var tradingPair2Response = responseList.First(r => r.IdTradingPair == tradingPair2.Id);
        tradingPair2Response.KlineData.Should().HaveCount(1);
    }

    [Fact]
    public async Task InsertKlineData_ShouldAddEntitiesToDatabaseAndReturnInsertedData()
    {
        // Arrange
        var tradingPair1 = await CreateTradingPairAsync();
        var tradingPair2 = await CreateTradingPairAsync();

        var klineDataList = new List<KlineDataCreationRequest>
        {
            _fixture
                .Build<KlineDataCreationRequest>()
                .With(x => x.IdTradingPair, tradingPair1.Id)
                .Create(),
            _fixture
                .Build<KlineDataCreationRequest>()
                .With(x => x.IdTradingPair, tradingPair1.Id)
                .Create(),
            _fixture
                .Build<KlineDataCreationRequest>()
                .With(x => x.IdTradingPair, tradingPair1.Id)
                .Create(),
            _fixture
                .Build<KlineDataCreationRequest>()
                .With(x => x.IdTradingPair, tradingPair2.Id)
                .Create(),
            _fixture
                .Build<KlineDataCreationRequest>()
                .With(x => x.IdTradingPair, tradingPair2.Id)
                .Create(),
        };

        // Act
        var result = await _repository.InsertKlineData(klineDataList);

        // Assert
        var entities = await _context.KlineData.ToListAsync();
        entities.Should().HaveCount(5);

        var responseList = result.ToList();
        responseList.Should().HaveCount(2);

        var tradingPair1Response = responseList.First(r => r.IdTradingPair == tradingPair1.Id);
        tradingPair1Response.KlineData.Should().HaveCount(3);

        var tradingPair2Response = responseList.First(r => r.IdTradingPair == tradingPair2.Id);
        tradingPair2Response.KlineData.Should().HaveCount(2);
    }

    [Fact]
    public async Task ReplaceAllKlineData_ShouldReplaceExistingDataAndReturnNewData()
    {
        // Arrange
        var tradingPair1 = await CreateTradingPairAsync();
        var tradingPair2 = await CreateTradingPairAsync();

        var existingData = new List<KlineDataEntity>
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
        await _context.KlineData.AddRangeAsync(existingData);
        await _context.SaveChangesAsync();

        var newTradingPair = await CreateTradingPairAsync();
        var newKlineData = new[]
        {
            _fixture
                .Build<KlineDataCreationRequest>()
                .With(x => x.IdTradingPair, newTradingPair.Id)
                .Create(),
            _fixture
                .Build<KlineDataCreationRequest>()
                .With(x => x.IdTradingPair, newTradingPair.Id)
                .Create(),
            _fixture
                .Build<KlineDataCreationRequest>()
                .With(x => x.IdTradingPair, newTradingPair.Id)
                .Create(),
        };

        // Act
        var result = await _repository.ReplaceAllKlineData(newKlineData);

        // Assert
        var entities = await _context.KlineData.ToListAsync();
        entities.Should().HaveCount(3);
        entities.Should().AllSatisfy(e => e.IdTradingPair.Should().Be(newTradingPair.Id));

        var responseList = result.ToList();
        responseList.Should().HaveCount(1);

        var response = responseList[0];
        response.IdTradingPair.Should().Be(newTradingPair.Id);
        response.KlineData.Should().HaveCount(3);
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
