namespace GUI_Crypto.Tests.Integration.Controllers;

public abstract class BaseControllerIntegrationTest(CustomWebApplicationFactory factory)
    : IAsyncLifetime
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
            Factory.SvcCoinsServerMock.ResetMappingsAsync(),
            Factory.SvcExternalServerMock.ResetMappingsAsync(),
            Factory.SvcKlineServerMock.ResetMappingsAsync()
        );
}
