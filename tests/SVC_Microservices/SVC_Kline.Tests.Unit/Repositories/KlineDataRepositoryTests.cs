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

    [Fact]
    public async Task InsertKlineData_ShouldAddEntityToDatabase()
    {
        // Arrange
        var klineData = _fixture.Create<KlineDataNew>();
        var klineDataEntity = Mapping.ToKlineDataEntity(klineData);

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

    [Fact]
    public async Task ReplaceAllKlineData_ShouldReplaceExistingData()
    {
        // Arrange
        var existingData = _fixture.CreateMany<KlineDataEntity>(5).ToList();
        await _context.KlineData.AddRangeAsync(existingData);
        await _context.SaveChangesAsync();

        var newKlineData = _fixture.CreateMany<KlineDataNew>(3).ToArray();
        var expectedData = newKlineData.Select(Mapping.ToKlineDataEntity);

        // Act
        await _repository.ReplaceAllKlineData(newKlineData);

        // Assert
        var entities = await _context.KlineData.ToListAsync();
        entities.Should().HaveCount(3);
        entities.Should().BeEquivalentTo(expectedData);
    }

    [Fact]
    public async Task ReplaceAllKlineData_ShouldHandleEmptyInput()
    {
        // Arrange
        var existingData = _fixture.CreateMany<KlineDataEntity>(5).ToList();
        await _context.KlineData.AddRangeAsync(existingData);
        await _context.SaveChangesAsync();

        var newKlineData = Array.Empty<KlineDataNew>();

        // Act
        await _repository.ReplaceAllKlineData(newKlineData);

        // Assert
        var entities = await _context.KlineData.ToListAsync();
        entities.Should().BeEmpty();
    }

    private static class Mapping
    {
        public static KlineDataEntity ToKlineDataEntity(KlineDataNew klineDataNew) => new()
        {
            IdTradePair = klineDataNew.IdTradePair,
            OpenTime = klineDataNew.OpenTime,
            OpenPrice = klineDataNew.OpenPrice,
            HighPrice = klineDataNew.HighPrice,
            LowPrice = klineDataNew.LowPrice,
            ClosePrice = klineDataNew.ClosePrice,
            Volume = klineDataNew.Volume,
            CloseTime = klineDataNew.CloseTime
        };
    }
}
