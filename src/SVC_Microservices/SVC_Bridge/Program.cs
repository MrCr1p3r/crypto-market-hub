using CryptoChartAnalyzer.ServiceDefaults;
using SharedLibrary.Infrastructure;
using SVC_Bridge.MicroserviceClients.SvcCoins;
using SVC_Bridge.MicroserviceClients.SvcExternal;
using SVC_Bridge.MicroserviceClients.SvcKline;
using SVC_Bridge.Services;
using SVC_Bridge.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient(
    "SvcCoinsClient",
    client =>
    {
        var baseAddress =
            builder.Configuration["Services:SvcCoinsClient:BaseUrl"] ?? "http://localhost:5161";
        client.BaseAddress = new Uri(baseAddress);
    }
);

builder.Services.AddHttpClient(
    "SvcExternalClient",
    client =>
    {
        var baseAddress =
            builder.Configuration["Services:SvcExternalClient:BaseUrl"] ?? "http://localhost:5135";
        client.BaseAddress = new Uri(baseAddress);
    }
);

builder.Services.AddHttpClient(
    "SvcKlineClient",
    client =>
    {
        var baseAddress =
            builder.Configuration["Services:SvcKlineClient:BaseUrl"] ?? "http://localhost:5117";
        client.BaseAddress = new Uri(baseAddress);
    }
);

builder.Services.AddScoped<ISvcCoinsClient, SvcCoinsClient>();
builder.Services.AddScoped<ISvcExternalClient, SvcExternalClient>();
builder.Services.AddScoped<ISvcKlineClient, SvcKlineClient>();

builder.Services.AddScoped<ICoinsService, CoinsService>();
builder.Services.AddScoped<IKlineDataService, KlineDataService>();
builder.Services.AddScoped<ITradingPairsService, TradingPairsService>();

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
await app.RunAsync();

public partial class Program { }
