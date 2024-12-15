using System.Net;
using System.Net.Http.Json;
using AutoFixture;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SVC_Coins.Models.Entities;
using SVC_Coins.Models.Input;
using SVC_Coins.Models.Output;
using SVC_Coins.Repositories;
using SVC_Coins.Tests.Integration.Factories;

namespace SVC_Coins.Tests.Integration.Controllers;

public class CoinsControllerTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    private readonly IFixture _fixture = new Fixture();

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
        var insertedCoin = coinsList!.FirstOrDefault(c =>
            c.Name == coinNew.Name && c.Symbol == coinNew.Symbol
        );
        insertedCoin.Should().NotBeNull();

        // Act
        var response = await _client.DeleteAsync($"/api/Coins/delete/{insertedCoin!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().Contain($"Coin with ID {insertedCoin.Id} deleted successfully.");

        getAllResponse = await _client.GetAsync("/api/Coins/getAll");
        coinsList = await getAllResponse.Content.ReadFromJsonAsync<IEnumerable<Coin>>();
        coinsList.Should().NotContain(c => c.Id == insertedCoin.Id);
    }

    [Fact]
    public async Task InsertTradingPair_ReturnsOk()
    {
        // Arrange
        var tradingPairNew = _fixture.Create<TradingPairNew>();

        // Act
        var response = await _client.PostAsJsonAsync(
            "/api/Coins/tradingPair/insert",
            tradingPairNew
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var insertedId = await response.Content.ReadFromJsonAsync<int>();
        insertedId.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetQuoteCoinsPrioritized_ShouldReturnOkWithPrioritizedList()
    {
        // Arrange
        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CoinsDbContext>();

            var coin1 = new CoinEntity
            {
                Name = "Bitcoin",
                Symbol = "BTC",
                QuoteCoinPriority = 2,
            };
            var coin2 = new CoinEntity
            {
                Name = "Ethereum",
                Symbol = "ETH",
                QuoteCoinPriority = 1,
            };
            var coin3 = new CoinEntity
            {
                Name = "Tether",
                Symbol = "USDT",
                QuoteCoinPriority = 3,
            };

            dbContext.Coins.AddRange(coin1, coin2, coin3);
            await dbContext.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync("/api/Coins/getQuoteCoinsPrioritized");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var prioritizedCoins = await response.Content.ReadFromJsonAsync<IEnumerable<Coin>>();
        prioritizedCoins.Should().NotBeNull();
        prioritizedCoins.Should().HaveCount(3);

        var orderedCoins = prioritizedCoins!.ToList();
        orderedCoins[0].Name.Should().Be("Ethereum");
        orderedCoins[1].Name.Should().Be("Bitcoin");
        orderedCoins[2].Name.Should().Be("Tether");
    }
}
