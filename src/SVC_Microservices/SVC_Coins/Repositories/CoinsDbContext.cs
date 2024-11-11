using Microsoft.EntityFrameworkCore;
using SVC_Coins.Models.Entities;

namespace SVC_Coins.Repositories;

/// <summary>
/// The DbContext class for managing database operations related to cryptocurrency coins.
/// </summary>
/// <param name="options">The options to configure the DbContext.</param>
public class CoinsDbContext(DbContextOptions<CoinsDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Gets or sets the DbSet for cryptocurrency coin entities.
    /// </summary>
    public DbSet<CoinEntity> Coins { get; set; }

    public DbSet<TradingPairEntity> TradingPairs { get; set; }

    /// <summary>
    /// Configures the model and relationships using Fluent API.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<CoinEntity>()
            .HasMany(c => c.TradingPairs)
            .WithOne(tp => tp.CoinMain)
            .HasForeignKey(tp => tp.IdCoinMain)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TradingPairEntity>()
            .HasOne(tp => tp.CoinQuote)
            .WithMany()
            .HasForeignKey(tp => tp.IdCoinQuote)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
