using System.Net;
using System.Text.Json;
using FluentAssertions;
using GUI_Crypto.Models.Input;
using GUI_Crypto.Models.Output;
using GUI_Crypto.Tests.Integration.Factories;
using SharedLibrary.Enums;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace GUI_Crypto.Tests.Integration.Controllers;

public class ChartControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public ChartControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();

        // Setup mock response for GetCoinsByIds
        _factory
            .CoinsServiceMock.Given(
                Request.Create().WithPath("/coins/byIds").WithParam("ids").UsingGet()
            )
            .RespondWith(
                Response
                    .Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(
                        """
                        [{
                            "id": 1,
                            "symbol": "BTC",
                            "name": "Bitcoin",
                            "tradingPairs": []
                        }]
                        """
                    )
            );

        // Setup mock response for GetKlineData from external service
        var defaultKlineData = new[]
        {
            new KlineDataExchange
            {
                OpenTime = DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeMilliseconds(),
                OpenPrice = 40000m,
                HighPrice = 41000m,
                LowPrice = 39000m,
                ClosePrice = 40500m,
                Volume = 100m,
                CloseTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            },
        };
        var serializedKlineData = JsonSerializer.Serialize(defaultKlineData);

        _factory
            .ExternalServiceMock.Given(Request.Create().WithPath("/exchanges/klineData").UsingGet())
            .RespondWith(
                Response
                    .Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(serializedKlineData)
            );
    }

    [Fact]
    public async Task Chart_ReturnsSuccessStatusCode()
    {
        // Arrange
        var formContent = new FormUrlEncodedContent(
            new[]
            {
                new KeyValuePair<string, string>("IdCoinMain", "1"),
                new KeyValuePair<string, string>("SymbolCoinMain", "BTC"),
                new KeyValuePair<string, string>("SymbolCoinQuote", "USDT"),
            }
        );

        // Act
        var response = await _client.PostAsync("/chart", formContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetKlineData_ReturnsExpectedData()
    {
        // Arrange
        var fixedTime = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var expectedKlineData = new[]
        {
            new KlineDataExchange
            {
                OpenTime = fixedTime.ToUnixTimeMilliseconds(),
                OpenPrice = 40000m,
                HighPrice = 41000m,
                LowPrice = 39000m,
                ClosePrice = 40500m,
                Volume = 100m,
                CloseTime = fixedTime.AddHours(1).ToUnixTimeMilliseconds(),
            },
        };

        // Update the mock setup with the same fixed data
        _factory
            .ExternalServiceMock.Given(Request.Create().WithPath("/exchanges/klineData").UsingGet())
            .RespondWith(
                Response
                    .Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(JsonSerializer.Serialize(expectedKlineData))
            );

        var request = new KlineDataRequest
        {
            CoinMainSymbol = "BTC",
            CoinQuoteSymbol = "USDT",
            Interval = ExchangeKlineInterval.FifteenMinutes,
            StartTime = fixedTime.DateTime.AddDays(-7),
            EndTime = fixedTime.DateTime,
        };

        var queryString =
            $"?coinMainSymbol={request.CoinMainSymbol}"
            + $"&coinQuoteSymbol={request.CoinQuoteSymbol}"
            + $"&interval={request.Interval}"
            + $"&startTime={Uri.EscapeDataString(request.StartTime.ToString("O"))}"
            + $"&endTime={Uri.EscapeDataString(request.EndTime.ToString("O"))}";

        // Act
        var response = await _client.GetAsync($"/chart/klines{queryString}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };
        var klineData = JsonSerializer.Deserialize<IEnumerable<KlineDataExchange>>(
            content,
            options
        );
        klineData.Should().BeEquivalentTo(expectedKlineData);
    }
}
