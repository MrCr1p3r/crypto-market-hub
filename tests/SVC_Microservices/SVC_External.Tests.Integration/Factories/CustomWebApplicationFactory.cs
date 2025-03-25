using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using WireMock.Client;
using WireMock.Net.Testcontainers;

namespace SVC_External.Tests.Integration.Factories;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly WireMockContainer _binanceWireMockContainer =
        new WireMockContainerBuilder().Build();

    private readonly WireMockContainer _bybitWireMockContainer =
        new WireMockContainerBuilder().Build();

    private readonly WireMockContainer _mexcWireMockContainer =
        new WireMockContainerBuilder().Build();

    private readonly WireMockContainer _coinGeckoWireMockContainer =
        new WireMockContainerBuilder().Build();

    // Expose mock servers for tests
    public IWireMockAdminApi BinanceServerMock { get; private set; } = null!;

    public IWireMockAdminApi BybitServerMock { get; private set; } = null!;

    public IWireMockAdminApi MexcServerMock { get; private set; } = null!;

    public IWireMockAdminApi CoinGeckoServerMock { get; private set; } = null!;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(
            (context, config) =>
            {
                config.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Services:BinanceClient:BaseUrl"] =
                            _binanceWireMockContainer.GetPublicUrl(),
                        ["Services:BybitClient:BaseUrl"] = _bybitWireMockContainer.GetPublicUrl(),
                        ["Services:MexcClient:BaseUrl"] = _mexcWireMockContainer.GetPublicUrl(),
                        ["Services:CoinGeckoClient:BaseUrl"] =
                            _coinGeckoWireMockContainer.GetPublicUrl(),
                    }
                );
            }
        );
    }

    public async Task InitializeAsync()
    {
        await Task.WhenAll(
            _binanceWireMockContainer.StartAsync(),
            _bybitWireMockContainer.StartAsync(),
            _mexcWireMockContainer.StartAsync(),
            _coinGeckoWireMockContainer.StartAsync()
        );

        BinanceServerMock = _binanceWireMockContainer.CreateWireMockAdminClient();
        BybitServerMock = _bybitWireMockContainer.CreateWireMockAdminClient();
        MexcServerMock = _mexcWireMockContainer.CreateWireMockAdminClient();
        CoinGeckoServerMock = _coinGeckoWireMockContainer.CreateWireMockAdminClient();
    }

    public new async Task DisposeAsync()
    {
        await _binanceWireMockContainer.DisposeAsync();
        await _bybitWireMockContainer.DisposeAsync();
        await _mexcWireMockContainer.DisposeAsync();
        await _coinGeckoWireMockContainer.DisposeAsync();
        await base.DisposeAsync();
    }
}
