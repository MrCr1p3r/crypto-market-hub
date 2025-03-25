using SVC_External.Tests.Integration.Factories;

namespace SVC_External.Tests.Integration;

public abstract class BaseIntegrationTest(CustomWebApplicationFactory factory) : IAsyncLifetime
{
    private protected CustomWebApplicationFactory Factory { get; } = factory;

    private protected HttpClient Client { get; } = factory.CreateClient();

    public virtual Task InitializeAsync() => Task.CompletedTask;

    public virtual async Task DisposeAsync()
    {
        await ClearWireMockMappings();
    }

    private async Task ClearWireMockMappings() =>
        await Task.WhenAll(
            Factory.BinanceServerMock.ResetMappingsAsync(),
            Factory.BybitServerMock.ResetMappingsAsync(),
            Factory.MexcServerMock.ResetMappingsAsync(),
            Factory.CoinGeckoServerMock.ResetMappingsAsync()
        );
}
