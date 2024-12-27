using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SVC_Coins.Repositories;
using Testcontainers.MsSql;

namespace SVC_Coins.Tests.Integration.Factories;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _databaseContainer = new MsSqlBuilder()
        .WithPassword("Password12!")
        .WithEnvironment("ACCEPT_EULA", "Y")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the app's database context
            var descriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<CoinsDbContext>)
            );
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<CoinsDbContext>(options =>
            {
                options.UseSqlServer(_databaseContainer.GetConnectionString());
            });
        });
    }

    public async Task InitializeAsync()
    {
        // 1) Start the container
        await _databaseContainer.StartAsync();

        // 2) Apply EF Core migrations to create the schema
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CoinsDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
    }

    public new async Task DisposeAsync()
    {
        await _databaseContainer.StopAsync();
    }
}
