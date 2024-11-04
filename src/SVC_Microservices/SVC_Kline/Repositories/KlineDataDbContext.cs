using Microsoft.EntityFrameworkCore;
using SVC_Kline.Models.Entities;

namespace SVC_Kline.Repositories;

/// <summary>
/// The DbContext class for managing database operations related to Kline data.
/// </summary>
/// <remarks>
/// Initializes a new instance of the KlineDataDbContext class.
/// </remarks>
/// <param name="options">The options to configure the DbContext.</param>
public class KlineDataDbContext(DbContextOptions<KlineDataDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Gets or sets the DbSet for Kline data entities.
    /// </summary>
    public DbSet<KlineDataEntity> KlineData { get; set; }

    /// <summary>
    /// Configures the model and relationships using Fluent API.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}
