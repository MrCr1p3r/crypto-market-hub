using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using GUI_Crypto.Models.Input;
using GUI_Crypto.Tests.Integration.Factories;
using SVC_External.Models.Output;
using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace GUI_Crypto.Tests.Integration.Controllers;

public class OverviewControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public OverviewControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();

        // Setup mock responses
        _factory
            .CoinsServiceMock.Given(Request.Create().WithPath("/coins/all").UsingGet())
            .RespondWith(
                Response
                    .Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody("[]")
            );

        _factory
            .KlineServiceMock.Given(Request.Create().WithPath("/kline/all").UsingGet())
            .RespondWith(
                Response
                    .Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody("{}")
            );
    }

    [Fact]
    public async Task RenderOverview_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/overview");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetListedCoins_ReturnsSuccessStatusCodeAndData()
    {
        // Arrange
        var expectedCoins = new ListedCoins
        {
            BinanceCoins = ["BTC", "ETH"],
            BybitCoins = ["BTC", "USDT"],
            MexcCoins = ["ETH", "BNB"],
        };

        _factory
            .ExternalServiceMock.Given(
                Request.Create().WithPath("/exchanges/allListedCoins").UsingGet()
            )
            .RespondWith(
                Response
                    .Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(JsonSerializer.Serialize(expectedCoins))
            );

        // Act
        var response = await _client.GetAsync("/listed-coins");
        var content = await response.Content.ReadFromJsonAsync<ListedCoins>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().NotBeNull();
        content.Should().BeEquivalentTo(expectedCoins);
    }

    [Fact]
    public async Task CreateCoin_WhenSuccessful_ReturnsOk()
    {
        // Arrange
        var coin = new CoinNew
        {
            Symbol = "BTC",
            Name = "Bitcoin",
            QuoteCoinPriority = 1,
            IsStablecoin = false,
        };

        _factory
            .CoinsServiceMock.Given(
                Request
                    .Create()
                    .WithPath("/coins/insert")
                    .WithBody(new JsonMatcher(JsonSerializer.Serialize(coin), true))
                    .UsingPost()
            )
            .RespondWith(Response.Create().WithStatusCode(204));

        // Act
        var response = await _client.PostAsJsonAsync("/coin", coin);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateCoin_WhenCoinExists_ReturnsConflict()
    {
        // Arrange
        var coin = new CoinNew
        {
            Symbol = "USDT",
            Name = "Tether",
            QuoteCoinPriority = 2,
            IsStablecoin = true,
        };

        _factory
            .CoinsServiceMock.Given(
                Request
                    .Create()
                    .WithPath("/coins/insert")
                    .UsingPost()
                    .WithBody(new JsonMatcher(JsonSerializer.Serialize(coin), true))
            )
            .RespondWith(Response.Create().WithStatusCode(409));

        // Act
        var response = await _client.PostAsJsonAsync("/coin", coin);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DeleteCoin_ReturnsOk()
    {
        // Arrange
        const int coinId = 1;
        _factory
            .CoinsServiceMock.Given(Request.Create().WithPath($"/coins/{coinId}").UsingDelete())
            .RespondWith(Response.Create().WithStatusCode(200));

        // Act
        var response = await _client.DeleteAsync($"/coin/{coinId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAllCoins_ReturnsSuccessStatusCodeAndData()
    {
        // Act
        var response = await _client.GetAsync("/coins");
        var content = await response.Content.ReadFromJsonAsync<IEnumerable<object>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().NotBeNull();
    }
}
