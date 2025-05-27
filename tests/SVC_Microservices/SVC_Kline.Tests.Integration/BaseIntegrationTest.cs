using Microsoft.Extensions.DependencyInjection;
using SVC_Kline.Domain.Entities;
using SVC_Kline.Infrastructure;
using SVC_Kline.Tests.Integration.Factories;

namespace SVC_Kline.Tests.Integration;

public abstract class BaseIntegrationTest(CustomWebApplicationFactory factory) : IAsyncLifetime
{
    private protected CustomWebApplicationFactory Factory { get; } = factory;

    private protected HttpClient Client { get; } = factory.CreateClient();

    public virtual Task InitializeAsync() => Task.CompletedTask;

    public virtual async Task DisposeAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<KlineDataDbContext>();
        dbContext.KlineData.RemoveRange(dbContext.KlineData);
        dbContext.TradingPair.RemoveRange(dbContext.TradingPair);
        await dbContext.SaveChangesAsync();
    }

    protected KlineDataDbContext GetDbContext()
    {
        var scope = Factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<KlineDataDbContext>();
    }

    protected async Task<TradingPairEntity> CreateTradingPairAsync()
    {
        using var dbContext = GetDbContext();
        var tradingPair = new TradingPairEntity();
        dbContext.TradingPair.Add(tradingPair);
        await dbContext.SaveChangesAsync();
        return tradingPair;
    }

    protected async Task<IEnumerable<KlineDataEntity>> InsertKlineDataAsync(
        IEnumerable<KlineDataEntity> klineData
    )
    {
        using var dbContext = GetDbContext();
        dbContext.KlineData.AddRange(klineData);
        await dbContext.SaveChangesAsync();
        return klineData;
    }
}
