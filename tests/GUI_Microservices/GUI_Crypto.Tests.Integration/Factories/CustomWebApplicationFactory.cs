using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using WireMock.Server;

namespace GUI_Crypto.Tests.Integration.Factories;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private WireMockServer _coinsServiceMock = null!;
    private WireMockServer _klineServiceMock = null!;
    private WireMockServer _externalServiceMock = null!;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _coinsServiceMock = WireMockServer.Start();
        _klineServiceMock = WireMockServer.Start();
        _externalServiceMock = WireMockServer.Start();

        builder.ConfigureAppConfiguration(
            (context, config) =>
            {
                config.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Services:SvcCoinsClient:BaseUrl"] = _coinsServiceMock.Urls[0],
                        ["Services:SvcKlineClient:BaseUrl"] = _klineServiceMock.Urls[0],
                        ["Services:SvcExternalClient:BaseUrl"] = _externalServiceMock.Urls[0],
                    }
                );
            }
        );
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public new async Task DisposeAsync()
    {
        _coinsServiceMock?.Dispose();
        _klineServiceMock?.Dispose();
        _externalServiceMock?.Dispose();
        await base.DisposeAsync();
    }

    public WireMockServer CoinsServiceMock => _coinsServiceMock;
    public WireMockServer KlineServiceMock => _klineServiceMock;
    public WireMockServer ExternalServiceMock => _externalServiceMock;
}
