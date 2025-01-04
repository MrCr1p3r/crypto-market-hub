using SharedLibrary.Infrastructure;
using SVC_Bridge.Clients;
using SVC_Bridge.Clients.Interfaces;
using SVC_Bridge.DataCollectors;
using SVC_Bridge.DataCollectors.Interfaces;
using SVC_Bridge.DataDistributors;
using SVC_Bridge.DataDistributors.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient(
    "SvcCoinsClient",
    client =>
    {
        client.BaseAddress = new Uri("http://localhost:5161");
    }
);
builder.Services.AddHttpClient(
    "SvcExternalClient",
    client =>
    {
        client.BaseAddress = new Uri("http://localhost:5135");
    }
);
builder.Services.AddHttpClient(
    "SvcKlineClient",
    client =>
    {
        client.BaseAddress = new Uri("http://localhost:5117");
    }
);
builder.Services.AddScoped<ISvcCoinsClient, SvcCoinsClient>();
builder.Services.AddScoped<ISvcExternalClient, SvcExternalClient>();
builder.Services.AddScoped<ISvcKlineClient, SvcKlineClient>();
builder.Services.AddScoped<IKlineDataCollector, KlineDataCollector>();
builder.Services.AddScoped<IKlineDataDistributor, KlineDataDistributor>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

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
