using System.Net;
using System.Net.Http.Json;
using AutoFixture;
using FluentAssertions;
using SVC_Kline.Models.Input;
using SVC_Kline.Tests.Integration.Factories;

namespace SVC_Kline.Tests.Integration.Controllers;

public class KlineDataControllerIntegrationTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    private readonly IFixture _fixture = new Fixture();

    [Fact]
    public async Task InsertKlineData_ReturnsOk()
    {
        // Arrange
        var klineData = _fixture.Create<KlineData>();

        // Act
        var response = await _client.PostAsJsonAsync("/api/KlineData/insert", klineData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().Contain("Kline data inserted successfully.");
    }

    [Fact]
    public async Task InsertManyKlineData_ReturnsOk()
    {
        // Arrange
        var klineDataList = _fixture.CreateMany<KlineData>(5);

        // Act
        var response = await _client.PostAsJsonAsync("/api/KlineData/insertMany", klineDataList);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().Contain("Multiple Kline data entries inserted successfully.");
    }

    [Fact]
    public async Task GetAllKlineData_ShouldReturnOkWithList()
    {
        // Act
        var response = await _client.GetAsync("/api/KlineData/getAll");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var klineDataList = await response.Content.ReadFromJsonAsync<IEnumerable<KlineData>>();
        klineDataList.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteKlineDataForTradingPair_ReturnsOk()
    {
        // Arrange
        var klineData = _fixture.Create<KlineData>();
        await _client.PostAsJsonAsync("/api/KlineData/insert", klineData);

        // Act
        var response = await _client.DeleteAsync($"/api/KlineData/delete/{klineData.IdTradePair}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().Contain($"Kline data for trading pair ID {klineData.IdTradePair} deleted successfully.");
    }
}
