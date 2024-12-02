using System.Net;
using System.Net.Http.Json;
using AutoFixture;
using FluentAssertions;
using SVC_Kline.Models.Input;
using SVC_Kline.Models.Output;
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
        var klineData = _fixture.Create<KlineDataNew>();

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
        var klineDataList = _fixture.CreateMany<KlineDataNew>(5);

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
        var klineData = _fixture.Create<KlineDataNew>();
        await _client.PostAsJsonAsync("/api/KlineData/insert", klineData);

        // Act
        var response = await _client.DeleteAsync($"/api/KlineData/delete/{klineData.IdTradePair}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().Contain($"Kline data for trading pair ID {klineData.IdTradePair} deleted successfully.");
    }

    [Fact]
    public async Task ReplaceAllKlineData_ReturnsOk()
    {
        // Arrange
        var existingData = _fixture.CreateMany<KlineDataNew>(5).ToArray();
        await _client.PostAsJsonAsync("/api/KlineData/insertMany", existingData);

        var newKlineData = _fixture.CreateMany<KlineDataNew>(3).ToArray();

        // Act
        var response = await _client.PutAsJsonAsync("/api/KlineData/replaceAll", newKlineData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().Contain("All Kline data replaced successfully.");

        // Verify the data was replaced
        var getAllResponse = await _client.GetAsync("/api/KlineData/getAll");
        var klineDataList = await getAllResponse.Content.ReadFromJsonAsync<IEnumerable<KlineData>>();
        klineDataList.Should().HaveCount(3);
    }
}
