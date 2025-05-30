using Coravel;
using CryptoChartAnalyzer.ServiceDefaults;
using RabbitMQ.Client;
using SharedLibrary.Infrastructure;
using SVC_Scheduler.Infrastructure.DependencyInjection;
using SVC_Scheduler.Jobs;

var builder = WebApplication.CreateBuilder(args);

// Aspire service defaults (includes OpenTelemetry)
builder.AddServiceDefaults();

// DI composition (bottom-up)
builder
    .Services.AddInfrastructureServices()
    .AddHttpClients(builder.Configuration)
    .AddMicroserviceClients()
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
        .PreventOverlapping("MarketDataUpdate");

    // Kline Data Update Job - Every minute
    scheduler
        .Schedule<KlineDataUpdateJob>()
        .EveryMinute()
        .PreventOverlapping("GlobalDataUpdateLock");

    // Trading Pairs Update Job - Every 30 minutes
    scheduler
        .Schedule<TradingPairsUpdateJob>()
        .EveryMinute()
        .PreventOverlapping("GlobalDataUpdateLock");
});

await app.RunAsync();

public partial class Program { }
