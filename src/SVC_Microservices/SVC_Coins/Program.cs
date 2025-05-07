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

// Middleware pipeline
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseHttpsRedirection();

app.UseSwaggerPipeline();

app.MapDefaultEndpoints();
app.MapControllers();

await app.RunAsync();

public partial class Program { }
