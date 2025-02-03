using SharedLibrary.Infrastructure;
using SVC_External.Clients;
using SVC_External.Clients.Interfaces;
using SVC_External.DataCollectors;
using SVC_External.DataCollectors.Interfaces;

var builder = WebApplication.CreateBuilder(args);

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
        client.BaseAddress = new Uri("https://api.binance.com");
    }
);

builder.Services.AddHttpClient(
    "BybitClient",
    client =>
    {
        client.BaseAddress = new Uri("https://api.bybit.com");
    }
);

builder.Services.AddHttpClient(
    "MexcClient",
    client =>
    {
        client.BaseAddress = new Uri("https://api.mexc.com");
    }
);

builder.Services.AddScoped<IExchangeClient, BinanceClient>();
builder.Services.AddScoped<IExchangeClient, BybitClient>();
builder.Services.AddScoped<IExchangeClient, MexcClient>();
builder.Services.AddScoped<IExchangesDataCollector, ExchangesDataCollector>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// Map health check endpoints
app.MapDefaultEndpoints();

app.UseExceptionHandler();
app.UseStatusCodePages();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();

public partial class Program { }
