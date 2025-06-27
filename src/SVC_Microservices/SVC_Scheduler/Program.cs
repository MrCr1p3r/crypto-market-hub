using Coravel;
using CryptoMarketHub.ServiceDefaults;
using RabbitMQ.Client;
using SharedLibrary.Infrastructure;
using SVC_Scheduler.Infrastructure.DependencyInjection;
using SVC_Scheduler.Jobs.CacheWarmup;
using SVC_Scheduler.Jobs.UpdateJobs;

var builder = WebApplication.CreateBuilder(args);

// Aspire service defaults (includes OpenTelemetry)
builder.AddServiceDefaults();

// DI composition (bottom-up)
builder
    .Services.AddInfrastructureServices()
    .AddHttpClients(builder.Configuration)
    .AddRabbitMqPublisher(builder.Configuration)
    .AddScheduledJobs();

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Setup RabbitMQ infrastructure
var connectionFactory = app.Services.GetRequiredService<IConnectionFactory>();
await RabbitMq.SetupInfrastructureAsync(connectionFactory);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapDefaultEndpoints();

// Configure Coravel scheduled jobs
app.Services.UseScheduler(scheduler =>
#pragma warning disable S125 // Sections of code should not be commented out
#pragma warning disable S1135 // Track uses of "TODO" tags
{
    // Market Data Update Job - Every minute
    scheduler
        .Schedule<MarketDataUpdateJob>()
        .EveryMinute()
        .PreventOverlapping("MarketDataUpdate")
        .PreventOverlapping("CoinGeckoLock");

    // Kline Data Update Job - Every 5 minutes
    scheduler
        .Schedule<KlineDataUpdateJob>()
        .EveryFiveMinutes()
        .PreventOverlapping("KlineDataUpdate")
        .PreventOverlapping("TradingPairsKlineLock");

    // Spot Coins Cache Warmup Job - Every 15 minutes
    scheduler
        .Schedule<SpotCoinsCacheWarmupJob>()
        .EveryFifteenMinutes()
        .RunOnceAtStart()
        .PreventOverlapping("SpotCoinsCacheWarmup")
        .PreventOverlapping("CoinGeckoLock");

    // TODO: uncomment when ready
    // Trading Pairs Update Job - Every 30 minutes
    // scheduler
    //     .Schedule<TradingPairsUpdateJob>()
    //     .EveryThirtyMinutes()
    //     .PreventOverlapping("TradingPairsUpdate")
    //     .PreventOverlapping("TradingPairsKlineLock")
    //     .PreventOverlapping("CoinGeckoLock");
}
#pragma warning restore S1135 // Track uses of "TODO" tags
#pragma warning restore S125 // Sections of code should not be commented out
);

await app.RunAsync();

public partial class Program { }
