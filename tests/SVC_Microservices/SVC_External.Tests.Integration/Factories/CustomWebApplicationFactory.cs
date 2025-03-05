using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using WireMock.Server;

namespace SVC_External.Tests.Integration.Factories
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration(
                (context, config) =>
                {
                    config.AddInMemoryCollection(
                        new Dictionary<string, string?>
                        {
                            ["Services:BinanceClient:BaseUrl"] = BinanceServerMock.Urls[0],
                            ["Services:BybitClient:BaseUrl"] = BybitServerMock.Urls[0],
                            ["Services:MexcClient:BaseUrl"] = MexcServerMock.Urls[0],
                            ["Services:CoinGeckoClient:BaseUrl"] = CoinGeckoServerMock.Urls[0],
                        }
                    );
                }
            );
        }

        public Task InitializeAsync() => Task.CompletedTask;

        public new async Task DisposeAsync()
        {
            BinanceServerMock.Dispose();
            BybitServerMock.Dispose();
            MexcServerMock.Dispose();
            CoinGeckoServerMock.Dispose();
            await base.DisposeAsync();
        }

        // Expose mock servers for tests
        public WireMockServer BinanceServerMock { get; } = WireMockServer.Start();
        public WireMockServer BybitServerMock { get; } = WireMockServer.Start();
        public WireMockServer MexcServerMock { get; } = WireMockServer.Start();
        public WireMockServer CoinGeckoServerMock { get; } = WireMockServer.Start();
    }
}
