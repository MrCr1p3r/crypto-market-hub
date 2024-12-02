using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SVC_Kline.Repositories;

namespace SVC_Kline.Tests.Integration.Factories
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remove the existing KlineDataDbContext registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<KlineDataDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                var connection = new SqliteConnection("DataSource=:memory:");
                connection.Open();

                // Add the SQLite in-memory database context
                services.AddDbContext<KlineDataDbContext>(options =>
                {
                    options.UseSqlite(connection);
                });

                var serviceProvider = services.BuildServiceProvider();
                using var scope = serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<KlineDataDbContext>();
                dbContext.Database.EnsureCreated();
            });
        }
    }
}
