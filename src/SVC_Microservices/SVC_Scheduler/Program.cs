using Coravel;
using CryptoChartAnalyzer.ServiceDefaults;
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
{
    // Market Data Update Job - Every 30 seconds
    scheduler
        .Schedule<MarketDataUpdateJob>()
        .EveryThirtySeconds()
        .PreventOverlapping("MarketDataUpdate")
        .PreventOverlapping("CoinGeckoLock");

    // Kline Data Update Job - Every minute
    scheduler
        .Schedule<KlineDataUpdateJob>()
        .EveryMinute()
        .PreventOverlapping("KlineDataUpdate")
        .PreventOverlapping("TradingPairsKlineLock");

    // Spot Coins Cache Warmup Job - Every 2 minutes
    scheduler
        .Schedule<SpotCoinsCacheWarmupJob>()
        .Cron("*/2 * * * *")
        .RunOnceAtStart()
        .PreventOverlapping("SpotCoinsCacheWarmup")
        .PreventOverlapping("CoinGeckoLock");

    // Trading Pairs Update Job - Every 30 minutes TODO: uncomment when ready
    // scheduler
    //     .Schedule<TradingPairsUpdateJob>()
    //     .EveryThirtyMinutes()
    //     .PreventOverlapping("TradingPairsUpdate")
    //     .PreventOverlapping("TradingPairsKlineLock")
    //     .PreventOverlapping("CoinGeckoLock");
});

await app.RunAsync();

public partial class Program { }
