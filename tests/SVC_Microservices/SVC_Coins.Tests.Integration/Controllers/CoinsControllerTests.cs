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
    : IClassFixture<CustomWebApplicationFactory>,
        IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory = factory;
    private readonly HttpClient _client = factory.CreateClient();
    private readonly IFixture _fixture = new Fixture();

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        // Clean up the database after each test
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CoinsDbContext>();
        dbContext.Coins.RemoveRange(dbContext.Coins);
        dbContext.TradingPairs.RemoveRange(dbContext.TradingPairs);
        await dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task InsertCoin_ReturnsNoContent_WhenSuccessful()
    {
        // Arrange
        var coinNew = new CoinNew { Name = "Bitcoin", Symbol = "BTC" };

        // Act
        var response = await _client.PostAsJsonAsync("/coins/insert", coinNew);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var getAllResponse = await _client.GetAsync("/coins/all");
        var coinsList = await getAllResponse.Content.ReadFromJsonAsync<IEnumerable<Coin>>();
        coinsList.Should().ContainSingle(c => c.Name == coinNew.Name && c.Symbol == coinNew.Symbol);
    }

    [Fact]
    public async Task InsertCoin_ReturnsConflict_WhenCoinAlreadyExists()
    {
        // Arrange
        var coinNew = _fixture.Create<CoinNew>();
        // Insert once
        await _client.PostAsJsonAsync("/coins/insert", coinNew);

        // Act - Insert again
        var response = await _client.PostAsJsonAsync("/coins/insert", coinNew);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var message = await response.Content.ReadAsStringAsync();
        message.Should().Contain("already exists");
    }

    [Fact]
    public async Task GetAllCoins_ShouldReturnOkWithList()
    {
        // Arrange
        var coinNew = new CoinNew { Name = "Bitcoin", Symbol = "BTC" };
        await _client.PostAsJsonAsync("/coins/insert", coinNew);

        // Act
        var response = await _client.GetAsync("/coins/all");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var coinsList = await response.Content.ReadFromJsonAsync<IEnumerable<Coin>>();
        coinsList.Should().NotBeNull();
        coinsList.Should().ContainSingle(c => c.Name == coinNew.Name && c.Symbol == coinNew.Symbol);
    }

    [Fact]
    public async Task DeleteCoin_ReturnsNoContent_WhenSuccessful()
    {
        // Arrange: Insert a coin first
        var coinNew = new CoinNew { Name = "Bitcoin", Symbol = "BTC" };
        await _client.PostAsJsonAsync("/coins/insert", coinNew);
        var allCoinsResponse = await _client.GetAsync("/coins/all");
        var coinsList = await allCoinsResponse.Content.ReadFromJsonAsync<IEnumerable<Coin>>();
        var insertedCoin = coinsList!.First(c =>
            c.Name == coinNew.Name && c.Symbol == coinNew.Symbol
        );

        // Act: Delete the inserted coin
        var response = await _client.DeleteAsync($"/coins/{insertedCoin.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        allCoinsResponse = await _client.GetAsync("/coins/all");
        coinsList = await allCoinsResponse.Content.ReadFromJsonAsync<IEnumerable<Coin>>();
        coinsList.Should().NotContain(c => c.Id == insertedCoin.Id);
    }

    [Fact]
    public async Task DeleteCoin_ReturnsNotFound_WhenCoinDoesNotExist()
    {
        // Arrange
        var nonExistentId = int.MaxValue;

        // Act
        var response = await _client.DeleteAsync($"/coins/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var message = await response.Content.ReadAsStringAsync();
        message.Should().Contain("not found");
    }

    [Fact]
    public async Task InsertTradingPair_ReturnsOkWithId_WhenSuccessful()
    {
        // For trading pairs to be successfully inserted, both coins must exist.
        // Arrange: Insert two coins.
        var mainCoin = new CoinNew { Name = "Bitcoin", Symbol = "BTC" };
        var quoteCoin = new CoinNew { Name = "Ethereum", Symbol = "ETH" };

        await _client.PostAsJsonAsync("/coins/insert", mainCoin);
        await _client.PostAsJsonAsync("/coins/insert", quoteCoin);

        // Get inserted coins
        var getAllResponse = await _client.GetAsync("/coins/all");
        var coinsList = await getAllResponse.Content.ReadFromJsonAsync<IEnumerable<Coin>>();
        var mainCoinEntity = coinsList!.First(c =>
            c.Name == mainCoin.Name && c.Symbol == mainCoin.Symbol
        );
        var quoteCoinEntity = coinsList!.First(c =>
            c.Name == quoteCoin.Name && c.Symbol == quoteCoin.Symbol
        );

        var tradingPairNew = new TradingPairNew
        {
            IdCoinMain = mainCoinEntity.Id,
            IdCoinQuote = quoteCoinEntity.Id,
        };

        // Act
        var response = await _client.PostAsJsonAsync("/coins/tradingPairs/insert", tradingPairNew);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var insertedId = await response.Content.ReadFromJsonAsync<int>();
        insertedId.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task InsertTradingPair_ReturnsBadRequest_WhenInsertionFails()
    {
        // Attempt to insert a trading pair for non-existent coins
        var tradingPairNew = _fixture.Create<TradingPairNew>();

        var response = await _client.PostAsJsonAsync("/coins/tradingPairs/insert", tradingPairNew);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var message = await response.Content.ReadAsStringAsync();
        message.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetQuoteCoinsPrioritized_ShouldReturnOkWithPrioritizedList()
    {
        // Arrange: Insert coins directly into the DB with priorities
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CoinsDbContext>();

        var coin1 = new CoinsEntity
        {
            Name = "Bitcoin",
            Symbol = "BTC",
            QuoteCoinPriority = 2,
        };
        var coin2 = new CoinsEntity
        {
            Name = "Ethereum",
            Symbol = "ETH",
            QuoteCoinPriority = 1,
        };
        var coin3 = new CoinsEntity
        {
            Name = "Tether",
            Symbol = "USDT",
            QuoteCoinPriority = 3,
        };

        dbContext.Coins.AddRange(coin1, coin2, coin3);
        await dbContext.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/coins/quoteCoinsPrioritized");

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
