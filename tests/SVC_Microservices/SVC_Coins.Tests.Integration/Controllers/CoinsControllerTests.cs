using System.Net;
using System.Net.Http.Json;
using AutoFixture;
using FluentAssertions;
using SVC_Coins.Models.Input;
using SVC_Coins.Models.Output;
using SVC_Coins.Tests.Integration.Factories;

namespace SVC_Coins.Tests.Integration.Controllers;

public class CoinsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly IFixture _fixture;

    public CoinsControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _fixture = new Fixture();
    }

    [Fact]
    public async Task InsertCoin_ReturnsOk()
    {
        // Arrange
        var coinNew = _fixture.Create<CoinNew>();

        // Act
        var response = await _client.PostAsJsonAsync("/api/Coins/insert", coinNew);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().Contain("Coin inserted successfully.");
    }

    [Fact]
    public async Task GetAllCoins_ShouldReturnOkWithList()
    {
        // Arrange
        var coinNew = _fixture.Create<CoinNew>();
        await _client.PostAsJsonAsync("/api/Coins/insert", coinNew);

        // Act
        var response = await _client.GetAsync("/api/Coins/getAll");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var coinsList = await response.Content.ReadFromJsonAsync<IEnumerable<Coin>>();
        coinsList.Should().NotBeNull();
        coinsList.Should().ContainSingle(c => c.Name == coinNew.Name && c.Symbol == coinNew.Symbol);
    }

    [Fact]
    public async Task DeleteCoin_ReturnsOk()
    {
        // Arrange
        var coinNew = _fixture.Create<CoinNew>();
        await _client.PostAsJsonAsync("/api/Coins/insert", coinNew);

        var getAllResponse = await _client.GetAsync("/api/Coins/getAll");
        var coinsList = await getAllResponse.Content.ReadFromJsonAsync<IEnumerable<Coin>>();
        var insertedCoin = coinsList!.FirstOrDefault(c => c.Name == coinNew.Name && c.Symbol == coinNew.Symbol);
        insertedCoin.Should().NotBeNull();

        // Act
        var response = await _client.DeleteAsync($"/api/Coins/delete/{insertedCoin.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().Contain($"Coin with ID {insertedCoin.Id} deleted successfully.");

        getAllResponse = await _client.GetAsync("/api/Coins/getAll");
        coinsList = await getAllResponse.Content.ReadFromJsonAsync<IEnumerable<Coin>>();
        coinsList.Should().NotContain(c => c.Id == insertedCoin.Id);
    }
}
