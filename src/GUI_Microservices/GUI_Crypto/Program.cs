using CryptoChartAnalyzer.ServiceDefaults;
using GUI_Crypto.MicroserviceClients.SvcCoins;
using GUI_Crypto.MicroserviceClients.SvcExternal;
using GUI_Crypto.MicroserviceClients.SvcKline;
using GUI_Crypto.Services;
using GUI_Crypto.Services.Interfaces;
using GUI_Crypto.ViewModels;
using SharedLibrary.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddControllersWithViews();

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

builder.Services.AddHttpClient(
    "SvcBridgeClient",
    client =>
    {
        var baseAddress =
            builder.Configuration["Services:SvcBridgeClient:BaseUrl"] ?? "http://localhost:5109";
        client.BaseAddress = new Uri(baseAddress);
        client.Timeout = TimeSpan.FromMinutes(5);
    }
);

builder.Services.AddScoped<ISvcCoinsClient, SvcCoinsClient>();
builder.Services.AddScoped<ISvcExternalClient, SvcExternalClient>();
builder.Services.AddScoped<ISvcKlineClient, SvcKlineClient>();

// Register new domain-specific services
builder.Services.AddScoped<IOverviewService, OverviewService>();
builder.Services.AddScoped<IChartService, ChartService>();

// Keep the view model factory
builder.Services.AddScoped<ICryptoViewModelFactory, CryptoViewModelFactory>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// Map health check endpoints
app.MapDefaultEndpoints();

app.UseExceptionHandler();
app.UseStatusCodePages();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

await app.RunAsync();

public partial class Program { }
