using System.Net;
using System.Text;
using System.Text.Json;
using AutoFixture;
using FluentAssertions;
using Moq;
using SharedLibrary.Enums;
using SVC_Bridge.Clients.Interfaces;
using SVC_Bridge.DataCollectors.Interfaces;
using SVC_Bridge.Models.Input;
using SVC_Bridge.Tests.Integration.Factories;

namespace SVC_Bridge.Tests.Integration.Controllers;

public class KlineDataControllerTests
{
    private readonly HttpClient _client;
    private readonly Fixture _fixture;
    private readonly Mock<IKlineDataCollector> _mockKlineDataCollector;
    private readonly Mock<ISvcKlineClient> _mockKlineClient;
    private readonly KlineDataRequest _validRequest;

    public KlineDataControllerTests()
    {
        _fixture = new Fixture();
        _mockKlineDataCollector = new Mock<IKlineDataCollector>();
        _mockKlineClient = new Mock<ISvcKlineClient>();

        _validRequest = new KlineDataRequest
        {
            CoinMainSymbol = "BTC",
            CoinQuoteSymbol = "USDT",
            Interval = ExchangeKlineInterval.FourHours,
            StartTime = DateTime.UtcNow.AddDays(-7),
            EndTime = DateTime.UtcNow,
            Limit = 100,
        };

        _mockKlineDataCollector
            .Setup(dc => dc.CollectEntireKlineData(It.IsAny<KlineDataUpdateRequest>()))
            .ReturnsAsync(_fixture.CreateMany<KlineDataNew>(5));

        var factory = new CustomWebApplicationFactory(
            _mockKlineDataCollector.Object,
            _mockKlineClient.Object
        );
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task UpdateEntireKlineData_ShouldReturnOkWhenDataIsCollectedAndUpdated()
    {
        // Arrange
        var content = new StringContent(
            JsonSerializer.Serialize(_validRequest),
            Encoding.UTF8,
            "application/json"
        );

        // Act
        var response = await _client.PostAsync("/bridge/kline/updateEntireKlineData", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("Kline data updated successfully.");
    }

    [Fact]
    public async Task UpdateEntireKlineData_ShouldReturnBadRequestWhenNoDataCollected()
    {
        // Arrange
        _mockKlineDataCollector
            .Setup(dc => dc.CollectEntireKlineData(It.IsAny<KlineDataUpdateRequest>()))
            .ReturnsAsync([]);

        var content = new StringContent(
            JsonSerializer.Serialize(_validRequest),
            Encoding.UTF8,
            "application/json"
        );

        // Act
        var response = await _client.PostAsync("/bridge/kline/updateEntireKlineData", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("No kline data was collected.");
    }
}
