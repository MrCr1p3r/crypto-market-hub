using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SVC_Kline.Infrastructure.Configuration;

namespace SVC_Kline.Infrastructure.DependencyInjection;

/// <summary>
/// Extension methods for configuring database services.
/// </summary>
public static class DatabaseExtensions
{
    /// <summary>
    /// Adds the database context and configures its connection string.
    /// </summary>
    /// <param name="services">The IServiceCollection to add services to.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The updated IServiceCollection.</returns>
    public static IServiceCollection AddDatabaseServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        // Bind DatabaseSettings from configuration
        var databaseSettingsSection = configuration.GetSection(DatabaseSettings.SectionName);
        services.Configure<DatabaseSettings>(databaseSettingsSection);

        // Register DbContext using the bound settings
        services.AddDbContext<KlineDataDbContext>(
            (serviceProvider, options) =>
            {
                var databaseSettings = serviceProvider
                    .GetRequiredService<IOptions<DatabaseSettings>>()
                    .Value;

                options.UseSqlServer(
                    $"Server={databaseSettings.Server};"
                        + $"Database={databaseSettings.Name};"
                        + $"User Id={databaseSettings.User};"
                        + $"Password={databaseSettings.Password};"
                        + "MultipleActiveResultSets=true;TrustServerCertificate=true;"
                );
            }
        );

        return services;
    }
}
