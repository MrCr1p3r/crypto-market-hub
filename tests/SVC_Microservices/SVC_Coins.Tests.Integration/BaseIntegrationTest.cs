using Microsoft.Extensions.DependencyInjection;
using Respawn;
using SVC_Coins.Infrastructure;
using SVC_Coins.Tests.Integration.Factories;

namespace SVC_Coins.Tests.Integration;

public abstract class BaseIntegrationTest(CustomWebApplicationFactory factory) : IAsyncLifetime
{
    private protected CustomWebApplicationFactory Factory { get; } = factory;

    private protected HttpClient Client { get; } = factory.CreateClient();

    private Respawner _respawner = null!;

    public virtual async Task InitializeAsync()
    {
        var connectionString = Factory.GetConnectionString();
        _respawner = await Respawner.CreateAsync(
            connectionString,
            new RespawnerOptions { DbAdapter = DbAdapter.SqlServer, WithReseed = true }
        );

        await _respawner.ResetAsync(connectionString);
    }

    public virtual async Task DisposeAsync() => await Task.CompletedTask;

    protected CoinsDbContext GetDbContext()
    {
        var scope = Factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<CoinsDbContext>();
    }
}
