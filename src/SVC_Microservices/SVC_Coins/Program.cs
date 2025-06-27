using CryptoMarketHub.ServiceDefaults;
using SVC_Coins.Infrastructure;
using SVC_Coins.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Aspire service defaults (includes OpenTelemetry)
builder.AddServiceDefaults();

// DI composition (bottom-up)
builder
    .Services.AddInfrastructureServices()
    .AddDatabaseServices(builder.Configuration)
    .AddRepositories()
    .AddApplicationServices()
    .AddInputValidationServices()
    .AddWebApiServices();

var app = builder.Build();

// Ensure database exists and create all tables
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<CoinsDbContext>();
    await context.Database.EnsureCreatedAsync();
}

// Middleware pipeline
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseHttpsRedirection();

app.UseSwaggerPipeline();

app.MapDefaultEndpoints();
app.MapControllers();

await app.RunAsync();

public partial class Program { }
