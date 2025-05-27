using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using WireMock.Client;
using WireMock.Net.Testcontainers;

namespace SVC_Bridge.Tests.Integration.Factories;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly WireMockContainer _svcCoinsWireMockContainer =
        new WireMockContainerBuilder().Build();

    private readonly WireMockContainer _svcExternalWireMockContainer =
        new WireMockContainerBuilder().Build();

    // Expose mock servers for tests
    public IWireMockAdminApi SvcCoinsServerMock { get; private set; } = null!;

    public IWireMockAdminApi SvcExternalServerMock { get; private set; } = null!;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(
            (context, config) =>
            {
                config.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Services:SvcCoinsClient:BaseUrl"] =
                            _svcCoinsWireMockContainer.GetPublicUrl(),
                        ["Services:SvcExternalClient:BaseUrl"] =
                            _svcExternalWireMockContainer.GetPublicUrl(),
                    }
                );
            }
        );
    }

    public async Task InitializeAsync()
    {
        await Task.WhenAll(
            _svcCoinsWireMockContainer.StartAsync(),
            _svcExternalWireMockContainer.StartAsync()
        );

        SvcCoinsServerMock = _svcCoinsWireMockContainer.CreateWireMockAdminClient();
        SvcExternalServerMock = _svcExternalWireMockContainer.CreateWireMockAdminClient();
    }

    public new async Task DisposeAsync()
    {
        await _svcCoinsWireMockContainer.DisposeAsync();
        await _svcExternalWireMockContainer.DisposeAsync();
        await base.DisposeAsync();
    }
}
