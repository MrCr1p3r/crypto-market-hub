using Microsoft.Extensions.DependencyInjection;
using SVC_Coins.Models.Entities;
using SVC_Coins.Repositories;
using SVC_Coins.Tests.Integration.Factories;

namespace SVC_Coins.Tests.Integration;

public abstract class BaseIntegrationTest(CustomWebApplicationFactory factory) : IAsyncLifetime
{
    protected readonly CustomWebApplicationFactory Factory = factory;
    protected readonly HttpClient Client = factory.CreateClient();

    public virtual Task InitializeAsync() => Task.CompletedTask;

    public virtual async Task DisposeAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CoinsDbContext>();
        dbContext.Coins.RemoveRange(dbContext.Coins);
        dbContext.TradingPairs.RemoveRange(dbContext.TradingPairs);
        await dbContext.SaveChangesAsync();
    }

    protected CoinsDbContext GetDbContext()
    {
        var scope = Factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<CoinsDbContext>();
    }

    protected async Task<IEnumerable<CoinsEntity>> InsertCoinsAsync(IEnumerable<CoinsEntity> coins)
    {
        using var dbContext = GetDbContext();
        dbContext.Coins.AddRange(coins);
        await dbContext.SaveChangesAsync();
        return coins;
    }
}
