using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using SVC_Kline.Mappings;
using SVC_Kline.Repositories;
using SVC_Kline.Repositories.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Load environment variables from the .env file
Env.TraversePath().Load();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAutoMapper(typeof(KlineDataMappingProfile));
builder.Services.AddDbContext<KlineDataDbContext>(options =>
    options.UseSqlServer(
        $"Server={Environment.GetEnvironmentVariable("DB_SERVER")};" +
        $"Database={Environment.GetEnvironmentVariable("DB_NAME")};" +
        $"User Id={Environment.GetEnvironmentVariable("DB_USER")};" +
        $"Password={Environment.GetEnvironmentVariable("DB_PASSWORD")};" +
        "MultipleActiveResultSets=true;TrustServerCertificate=true;"));

// Register the repository for dependency injection
builder.Services.AddScoped<IKlineDataRepository, KlineDataRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
