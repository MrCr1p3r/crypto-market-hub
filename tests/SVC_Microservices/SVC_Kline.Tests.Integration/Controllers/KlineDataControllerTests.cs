using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using AutoFixture;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SVC_Kline.ApiContracts.Requests;
using SVC_Kline.ApiContracts.Responses;
using SVC_Kline.Domain.Entities;
using SVC_Kline.Tests.Integration.Factories;

namespace SVC_Kline.Tests.Integration.Controllers;

public class KlineDataControllerTests(CustomWebApplicationFactory factory)
    : BaseIntegrationTest(factory),
        IClassFixture<CustomWebApplicationFactory>
{
    private readonly Fixture _fixture = new();

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
        var response = await Client.GetAsync("/kline");

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
    public async Task InsertManyKlineData_ReturnsOk_WhenSuccessful()
    {
        // Arrange
        var tradingPair = await CreateTradingPairAsync();
        var klineDataList = _fixture
            .Build<KlineDataCreationRequest>()
            .With(x => x.IdTradingPair, tradingPair.Id)
            .CreateMany(5);

        // Act
        var response = await Client.PostAsJsonAsync("/kline", klineDataList);

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
            .Build<KlineDataCreationRequest>()
            .With(x => x.IdTradingPair, newTradingPair.Id)
            .CreateMany(2)
            .ToArray();

        // Act
        var response = await Client.PutAsJsonAsync("/kline", newKlineData);

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
            .With(x => x.IdTradingPair, tradingPairId)
            .With(
                x => x.OpenPrice,
                _fixture.Create<decimal>().ToString(CultureInfo.InvariantCulture)
            )
            .With(
                x => x.HighPrice,
                _fixture.Create<decimal>().ToString(CultureInfo.InvariantCulture)
            )
            .With(
                x => x.LowPrice,
                _fixture.Create<decimal>().ToString(CultureInfo.InvariantCulture)
            )
            .With(
                x => x.ClosePrice,
                _fixture.Create<decimal>().ToString(CultureInfo.InvariantCulture)
            )
            .With(x => x.Volume, _fixture.Create<decimal>().ToString(CultureInfo.InvariantCulture))
            .Without(x => x.IdTradePairNavigation)
            .Create();
    }
}
