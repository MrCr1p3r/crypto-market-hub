using System.Net;
using FluentAssertions;
using GUI_Crypto.Tests.Integration.Factories;
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
                    .WithBody("[]")
            );
    }

    [Fact]
    public async Task Overview_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/overview");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Overview_ReturnsExpectedView()
    {
        // Act
        var response = await _client.GetAsync("/overview");

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("<title>Crypto Overview</title>");
    }
}
