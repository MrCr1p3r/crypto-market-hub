using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using AutoFixture;
using Cysharp.Web;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using SVC_External.Models.Input;
using SVC_External.Models.Output;
namespace SVC_External.Tests.Integration.Controllers;

public class ExchangesControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly IFixture _fixture = new Fixture();

    public ExchangesControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetKlineData_ShouldReturnOkWithData()//TODO: do I need to use the actual values for the request?
    {
        // Arrange
        var klineDataRequest = _fixture.Create<KlineDataRequest>();
        var options = new WebSerializerOptions(WebSerializerProvider.Default)
        {
            CultureInfo = CultureInfo.InvariantCulture
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
}
