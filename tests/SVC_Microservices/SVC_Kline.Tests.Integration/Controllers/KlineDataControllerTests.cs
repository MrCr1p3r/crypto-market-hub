using System.Net;
using System.Net.Http.Json;
using AutoFixture;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SVC_Kline.Models.Entities;
using SVC_Kline.Models.Input;
using SVC_Kline.Models.Output;
using SVC_Kline.Tests.Integration.Factories;

namespace SVC_Kline.Tests.Integration.Controllers;

public class KlineDataControllerTests(CustomWebApplicationFactory factory)
    : BaseIntegrationTest(factory),
        IClassFixture<CustomWebApplicationFactory>
{
    private readonly Fixture _fixture = new();

    [Fact]
    public async Task InsertKlineData_ReturnsOk_WhenSuccessful()
    {
        // Arrange
        var tradingPair = await CreateTradingPairAsync();
        var klineData = _fixture
            .Build<KlineDataNew>()
            .With(x => x.IdTradePair, tradingPair.Id)
            .Create();

        // Act
        var response = await Client.PostAsJsonAsync("/kline/insert", klineData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().Contain("Kline data inserted successfully.");

        // Verify in database
        using var dbContext = GetDbContext();
        var entity = await dbContext.KlineData.FirstOrDefaultAsync(k =>
            k.IdTradePair == klineData.IdTradePair
        );
        entity.Should().NotBeNull();
        entity!.OpenPrice.Should().Be(klineData.OpenPrice);
    }

    [Fact]
    public async Task InsertManyKlineData_ReturnsOk_WhenSuccessful()
    {
        // Arrange
        var tradingPair = await CreateTradingPairAsync();
        var klineDataList = _fixture
            .Build<KlineDataNew>()
            .With(x => x.IdTradePair, tradingPair.Id)
            .CreateMany(5);

        // Act
        var response = await Client.PostAsJsonAsync("/kline/insertMany", klineDataList);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().Contain("Multiple Kline data entries inserted successfully.");

        // Verify in database
        using var dbContext = GetDbContext();
        var entities = await dbContext.KlineData.ToListAsync();
        entities.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetAllKlineData_ReturnsOk_WithGroupedData()
    {
        // Arrange
        var tradingPair1 = await CreateTradingPairAsync();
        var tradingPair2 = await CreateTradingPairAsync();

        var klineDataEntities = new List<KlineDataEntity>
        {
            CreateKlineDataEntity(tradingPair1.Id),
            CreateKlineDataEntity(tradingPair1.Id),
            CreateKlineDataEntity(tradingPair2.Id),
        };
        await InsertKlineDataAsync(klineDataEntities);

        // Act
        var response = await Client.GetAsync("/kline/all");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var klineDataDict = await response.Content.ReadFromJsonAsync<
            IReadOnlyDictionary<int, IEnumerable<KlineData>>
        >();
        klineDataDict.Should().NotBeNull();
        klineDataDict!.Should().HaveCount(2);
        klineDataDict![tradingPair1.Id].Should().HaveCount(2);
        klineDataDict[tradingPair2.Id].Should().HaveCount(1);
    }

    [Fact]
    public async Task DeleteKlineDataForTradingPair_ReturnsOk_WhenSuccessful()
    {
        // Arrange
        var tradingPair = await CreateTradingPairAsync();
        var klineDataEntities = new List<KlineDataEntity>
        {
            CreateKlineDataEntity(tradingPair.Id),
            CreateKlineDataEntity(tradingPair.Id),
        };
        await InsertKlineDataAsync(klineDataEntities);

        // Act
        var response = await Client.DeleteAsync($"/kline/{tradingPair.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody
            .Should()
            .Contain($"Kline data for trading pair ID {tradingPair.Id} deleted successfully.");

        // Verify in database
        using var dbContext = GetDbContext();
        var remainingEntities = await dbContext
            .KlineData.Where(k => k.IdTradePair == tradingPair.Id)
            .ToListAsync();
        remainingEntities.Should().BeEmpty();
    }

    [Fact]
    public async Task ReplaceAllKlineData_ReturnsOk_WhenSuccessful()
    {
        // Arrange
        var tradingPair1 = await CreateTradingPairAsync();
        var tradingPair2 = await CreateTradingPairAsync();

        var existingData = new List<KlineDataEntity>
        {
            CreateKlineDataEntity(tradingPair1.Id),
            CreateKlineDataEntity(tradingPair1.Id),
            CreateKlineDataEntity(tradingPair2.Id),
        };
        await InsertKlineDataAsync(existingData);

        var newTradingPair = await CreateTradingPairAsync();
        var newKlineData = _fixture
            .Build<KlineDataNew>()
            .With(x => x.IdTradePair, newTradingPair.Id)
            .CreateMany(2)
            .ToArray();

        // Act
        var response = await Client.PutAsJsonAsync("/kline/replaceAll", newKlineData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().Contain("All Kline data replaced successfully.");

        // Verify the data was replaced
        using var dbContext = GetDbContext();
        var entities = await dbContext.KlineData.ToListAsync();
        entities.Should().HaveCount(2);
    }

    private KlineDataEntity CreateKlineDataEntity(int tradingPairId)
    {
        return _fixture
            .Build<KlineDataEntity>()
            .With(x => x.IdTradePair, tradingPairId)
            .Without(x => x.IdTradePairNavigation)
            .Create();
    }
}
