using System.Net;
using AutoFixture;
using FluentAssertions;
using Moq;
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

    public KlineDataControllerTests()
    {
        _fixture = new Fixture();
        _mockKlineDataCollector = new Mock<IKlineDataCollector>();
        _mockKlineClient = new Mock<ISvcKlineClient>();

        _mockKlineDataCollector
            .Setup(dc => dc.CollectEntireKlineData())
            .ReturnsAsync(_fixture.CreateMany<KlineDataNew>(5).ToList());

        var factory = new CustomWebApplicationFactory(
            _mockKlineDataCollector.Object,
            _mockKlineClient.Object
        );
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task UpdateEntireKlineData_ShouldReturnOkWhenDataIsCollectedAndUpdated()
    {
        // Act
        var response = await _client.PostAsync("/api/KlineData/updateEntireKlineData", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("Kline data updated successfully.");
    }

    [Fact]
    public async Task UpdateEntireKlineData_ShouldReturnBadRequestWhenNoDataCollected()
    {
        // Arrange
        _mockKlineDataCollector.Setup(dc => dc.CollectEntireKlineData()).ReturnsAsync([]);

        // Act
        var response = await _client.PostAsync("/api/KlineData/updateEntireKlineData", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("No kline data was collected.");
    }
}
