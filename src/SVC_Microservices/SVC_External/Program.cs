using System.Threading.RateLimiting;
using DotNetEnv;
using Microsoft.Extensions.Http.Resilience;
using SharedLibrary.Infrastructure;
using SVC_External.Clients.Exchanges;
using SVC_External.Clients.Exchanges.Interfaces;
using SVC_External.Clients.MarketDataProviders;
using SVC_External.Clients.MarketDataProviders.Interfaces;
using SVC_External.DataCollectors;
using SVC_External.DataCollectors.Interfaces;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;

var builder = WebApplication.CreateBuilder(args);

// Load environment variables
Env.TraversePath().Load();

// Add Aspire service defaults (includes OpenTelemetry)
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register containers for dependency injection
builder.Services.AddHttpClient(
    "BinanceClient",
    client =>
    {
        var baseUrl =
            builder.Configuration["Services:BinanceClient:BaseUrl"] ?? "https://api.binance.com";
        client.BaseAddress = new Uri(baseUrl);
    }
);

builder.Services.AddHttpClient(
    "BybitClient",
    client =>
    {
        var baseUrl =
            builder.Configuration["Services:BybitClient:BaseUrl"] ?? "https://api.bybit.com";
        client.BaseAddress = new Uri(baseUrl);
    }
);

builder.Services.AddHttpClient(
    "MexcClient",
    client =>
    {
        var baseUrl =
            builder.Configuration["Services:MexcClient:BaseUrl"] ?? "https://api.mexc.com";
        client.BaseAddress = new Uri(baseUrl);
    }
);

var rateLimiter = new SlidingWindowRateLimiter(
    new SlidingWindowRateLimiterOptions
    {
        Window = TimeSpan.FromSeconds(60),
        SegmentsPerWindow = 20,
        PermitLimit = 30,
        QueueLimit = int.MaxValue,
    }
);
builder.Services.AddSingleton(rateLimiter);
builder
    .Services.AddHttpClient(
        "CoinGeckoClient",
        client =>
        {
            var baseUrl =
                builder.Configuration["Services:CoinGeckoClient:BaseUrl"]
                ?? "https://api.coingecko.com";
            client.BaseAddress = new Uri(baseUrl);
            client.DefaultRequestHeaders.Add("accept", "application/json");
            client.DefaultRequestHeaders.Add("User-Agent", "SVC_External");
            client.DefaultRequestHeaders.Add(
                "x-cg-demo-api-key",
                Environment.GetEnvironmentVariable("COINGECKO_API_KEY")
            );
        }
    )
    .AddStandardResilienceHandler(options =>
    {
        options.RateLimiter = new HttpRateLimiterStrategyOptions
        {
            Name = "CoinGeckoClient-RateLimiter",
            RateLimiter = args =>
                rateLimiter.AcquireAsync(cancellationToken: args.Context.CancellationToken),
        };
    });

builder.Services.AddScoped<IExchangesClient, BinanceClient>();
builder.Services.AddScoped<IExchangesClient, BybitClient>();
builder.Services.AddScoped<IExchangesClient, MexcClient>();
builder.Services.AddScoped<ICoinGeckoClient, CoinGeckoClient>();
builder.Services.AddScoped<IExchangesDataCollector, ExchangesDataCollector>();

// Add exception handling
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Add caching
builder.AddRedisDistributedCache("redis"); // using Aspire
builder
    .Services.AddFusionCache()
    .WithDefaultEntryOptions(options => options.Duration = TimeSpan.FromMinutes(5))
    .WithSerializer(new FusionCacheSystemTextJsonSerializer())
    .WithRegisteredDistributedCache()
    .AsHybridCache();

var app = builder.Build();

// Map health check endpoints
app.MapDefaultEndpoints();

// Use exceptions handling
app.UseExceptionHandler();
app.UseStatusCodePages();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(config =>
    {
        config.ConfigObject.AdditionalItems["syntaxHighlight"] = new Dictionary<string, object>
        {
            ["activated"] = false,
        };
    });
}

app.UseHttpsRedirection();
app.MapControllers();
await app.RunAsync();

// Allows WebApplicationFactory to access the Program class.
public partial class Program { }
