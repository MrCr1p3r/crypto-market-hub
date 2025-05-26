using CryptoChartAnalyzer.ServiceDefaults;
using SVC_External.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults (includes OpenTelemetry)
builder.AddServiceDefaults();

// DI composition (bottom-up)
builder
    .Services.AddInfrastructureServices()
    .AddHttpClients(builder.Configuration)
    .AddExternalClients()
    .AddApplicationServices()
    .AddWebApiServices();

// Add caching with Redis
builder.AddRedisDistributedCache("redis"); // using Aspire
builder.Services.AddCachingServices();

var app = builder.Build();

// Middleware pipeline
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseHttpsRedirection();

app.UseSwaggerPipeline();

app.MapDefaultEndpoints();
app.MapControllers();

await app.RunAsync();

// Allows WebApplicationFactory to access the Program class.
public partial class Program { }
