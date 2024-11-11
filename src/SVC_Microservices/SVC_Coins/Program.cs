using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using SVC_Coins.Mappings;
using SVC_Coins.Repositories;
using SVC_Coins.Repositories.Interfaces;

//[assembly: InternalsVisibleTo("SVC_Coins.Tests.Integration")]

public class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        Env.TraversePath().Load();

        // Add services to the container.
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddControllers();
        // builder.Services.AddControllers().AddJsonOptions(options => TODO: It should now work without it but it does
        // {
        //     options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        // });
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddAutoMapper(typeof(CoinsMappingProfile));
        builder.Services.AddDbContext<CoinsDbContext>(options =>
            options.UseSqlServer(
                $"Server={Environment.GetEnvironmentVariable("DB_SERVER")};" +
                $"Database={Environment.GetEnvironmentVariable("DB_NAME")};" +
                $"User Id={Environment.GetEnvironmentVariable("DB_USER")};" +
                $"Password={Environment.GetEnvironmentVariable("DB_PASSWORD")};" +
                "MultipleActiveResultSets=true;TrustServerCertificate=true;").LogTo(Console.WriteLine, LogLevel.Information));

        // Register the repository for dependency injection
        builder.Services.AddScoped<ICoinsRepository, CoinsRepository>();

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
    }
}