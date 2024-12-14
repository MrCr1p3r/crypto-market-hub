using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using AutoFixture;
using Cysharp.Web;
using FluentAssertions;
using Moq;
using SVC_External.DataCollectors.Interfaces;
using SVC_External.Models.Input;
using SVC_External.Models.Output;
using SVC_External.Tests.Integration.Factories;

namespace SVC_External.Tests.Integration.Controllers;

public class ExchangesControllerIntegrationTests
{
    private readonly HttpClient _client;
    private readonly IFixture _fixture;
    private readonly Mock<IExchangesDataCollector> _mockDataCollector;

    public ExchangesControllerIntegrationTests()
    {
        _fixture = new Fixture();
        _mockDataCollector = new Mock<IExchangesDataCollector>();

        _mockDataCollector
            .Setup(dc => dc.GetAllListedCoins())
            .ReturnsAsync(_fixture.Create<ListedCoins>());

        _mockDataCollector
            .Setup(dc => dc.GetKlineData(It.IsAny<KlineDataRequest>()))
            .ReturnsAsync(_fixture.CreateMany<KlineData>(5));

        var factory = new CustomWebApplicationFactory(_mockDataCollector.Object);
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetKlineData_ShouldReturnOkWithData()
    {
        // Arrange
        var klineDataRequest = _fixture.Create<KlineDataRequest>();
        var options = new WebSerializerOptions(WebSerializerProvider.Default)
        {
            CultureInfo = CultureInfo.InvariantCulture,
        };
        var queryString = WebSerializer.ToQueryString(klineDataRequest, options);

        // Act
        var response = await _client.GetAsync($"/api/Exchanges/klineData?{queryString}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var klineDataList = await response.Content.ReadFromJsonAsync<IEnumerable<KlineData>>();
        klineDataList.Should().NotBeNull();
        klineDataList.Should().AllBeAssignableTo<KlineData>();
    }

    [Fact]
    public async Task GetAllListedCoins_ShouldReturnOkWithData()
    {
        // Act
        var response = await _client.GetAsync("/api/Exchanges/allListedCoins");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var coinsList = await response.Content.ReadFromJsonAsync<ListedCoins>();
        coinsList.Should().NotBeNull();
        coinsList.Should().BeAssignableTo<ListedCoins>();
    }
}
