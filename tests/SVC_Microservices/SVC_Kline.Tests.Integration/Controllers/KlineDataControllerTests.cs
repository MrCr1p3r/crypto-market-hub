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
        var klineDataResponses = await response.Content.ReadFromJsonAsync<
            IEnumerable<KlineDataResponse>
        >();
        klineDataResponses.Should().NotBeNull();

        var responseList = klineDataResponses!.ToList();
        responseList.Should().HaveCount(2);

        var tradingPair1Response = responseList.First(r => r.IdTradingPair == tradingPair1.Id);
        tradingPair1Response.Klines.Should().HaveCount(2);

        var tradingPair2Response = responseList.First(r => r.IdTradingPair == tradingPair2.Id);
        tradingPair2Response.Klines.Should().HaveCount(1);
    }

    [Fact]
    public async Task InsertKlineData_ReturnsOk_WithInsertedData()
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
        var response = await Client.PostAsJsonAsync("/kline", klineDataList);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var klineDataResponses = await response.Content.ReadFromJsonAsync<
            IEnumerable<KlineDataResponse>
        >();
        klineDataResponses.Should().NotBeNull();

        var responseList = klineDataResponses!.ToList();
        responseList.Should().HaveCount(2);

        var tradingPair1Response = responseList.First(r => r.IdTradingPair == tradingPair1.Id);
        tradingPair1Response.Klines.Should().HaveCount(3);

        var tradingPair2Response = responseList.First(r => r.IdTradingPair == tradingPair2.Id);
        tradingPair2Response.Klines.Should().HaveCount(2);

        // Verify in database
        using var dbContext = GetDbContext();
        var entities = await dbContext.KlineData.ToListAsync();
        entities.Should().HaveCount(5);
    }

    [Fact]
    public async Task ReplaceAllKlineData_ReturnsOk_WithReplacedData()
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
        };

        // Act
        var response = await Client.PutAsJsonAsync("/kline", newKlineData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var klineDataResponses = await response.Content.ReadFromJsonAsync<
            IEnumerable<KlineDataResponse>
        >();
        klineDataResponses.Should().NotBeNull();

        var responseList = klineDataResponses!.ToList();
        responseList.Should().HaveCount(1);

        var response1 = responseList[0];
        response1.IdTradingPair.Should().Be(newTradingPair.Id);
        response1.Klines.Should().HaveCount(2);

        // Verify the data was replaced
        using var dbContext = GetDbContext();
        var entities = await dbContext.KlineData.ToListAsync();
        entities.Should().HaveCount(2);
        entities.Should().AllSatisfy(e => e.IdTradingPair.Should().Be(newTradingPair.Id));
    }

    private KlineDataEntity CreateKlineDataEntity(int tradingPairId)
    {
        return _fixture
            .Build<KlineDataEntity>()
            .With(x => x.IdTradingPair, tradingPairId)
            .With(x => x.OpenPrice, _fixture.Create<string>())
            .With(x => x.HighPrice, _fixture.Create<string>())
            .With(x => x.LowPrice, _fixture.Create<string>())
            .With(x => x.ClosePrice, _fixture.Create<string>())
            .With(x => x.Volume, _fixture.Create<long>().ToString(CultureInfo.InvariantCulture))
            .Without(x => x.IdTradePairNavigation)
            .Create();
    }
}
