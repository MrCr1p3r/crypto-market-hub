using AutoFixture;
using AutoFixture.AutoMoq;
using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SVC_Kline.Models.Entities;
using SVC_Kline.Models.Input;
using SVC_Kline.Models.Output;
using SVC_Kline.Repositories;

namespace SVC_Kline.Tests.Unit.Repositories;

public class KlineDataRepositoryTests
{
    private readonly IFixture _fixture;
    private readonly KlineDataDbContext _context;
    private readonly IMapper _mapper;
    private readonly KlineDataRepository _repository;

    public KlineDataRepositoryTests()
    {
        _fixture = new Fixture().Customize(new AutoMoqCustomization());

        var options = new DbContextOptionsBuilder<KlineDataDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new KlineDataDbContext(options);

        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<KlineDataNew, KlineDataEntity>();
            cfg.CreateMap<KlineDataEntity, KlineData>();
        });
        _mapper = config.CreateMapper();

        _repository = new KlineDataRepository(_context, _mapper);
    }

    [Fact]
    public async Task InsertKlineData_ShouldAddEntityToDatabase()
    {
        // Arrange
        var klineData = _fixture.Create<KlineDataNew>();
        var klineDataEntity = _mapper.Map<KlineDataEntity>(klineData);

        // Act
        await _repository.InsertKlineData(klineData);

        // Assert
        var entity = await _context.KlineData.FirstOrDefaultAsync(e => e.IdTradePair == klineData.IdTradePair);
        entity.Should().NotBeNull();
        entity!.OpenPrice.Should().Be(klineDataEntity.OpenPrice);
    }

    [Fact]
    public async Task InsertManyKlineData_ShouldAddEntitiesToDatabase()
    {
        // Arrange
        var klineDataList = _fixture.CreateMany<KlineDataNew>(5).ToList();

        // Act
        await _repository.InsertManyKlineData(klineDataList);

        // Assert
        var entities = await _context.KlineData.ToListAsync();
        entities.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetAllKlineData_ShouldReturnAllEntities()
    {
        // Arrange
        var klineDataEntities = _fixture.CreateMany<KlineDataEntity>(3).ToList();
        await _context.KlineData.AddRangeAsync(klineDataEntities);
        await _context.SaveChangesAsync();

        // Act
        var klineDataList = await _repository.GetAllKlineData();

        // Assert
        klineDataList.Should().HaveCount(3);
    }

    [Fact]
    public async Task DeleteKlineDataForTradingPair_ShouldRemoveEntities()
    {
        // Arrange
        var klineDataEntities = _fixture.CreateMany<KlineDataEntity>(3).ToList();
        klineDataEntities[0].IdTradePair = 1;
        klineDataEntities[1].IdTradePair = 1;
        klineDataEntities[2].IdTradePair = 2;
        await _context.KlineData.AddRangeAsync(klineDataEntities);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteKlineDataForTradingPair(1);

        // Assert
        var remainingEntities = await _context.KlineData.ToListAsync();
        remainingEntities.Should().ContainSingle();
        remainingEntities[0].IdTradePair.Should().Be(2);
    }
}
