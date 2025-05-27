using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SVC_Kline.Infrastructure;
using Testcontainers.MsSql;

namespace SVC_Kline.Tests.Integration.Factories;

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
            var descriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<KlineDataDbContext>)
            );
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<KlineDataDbContext>(options =>
            {
                options.UseSqlServer(_databaseContainer.GetConnectionString());
            });
        });
    }

    public async Task InitializeAsync()
    {
        await _databaseContainer.StartAsync();

        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<KlineDataDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
    }

    public new async Task DisposeAsync()
    {
        await _databaseContainer.DisposeAsync();
    }
}
