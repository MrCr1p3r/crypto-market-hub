using Microsoft.EntityFrameworkCore;
using SVC_Coins.Domain.Entities;

namespace SVC_Coins.Infrastructure;

/// <summary>
/// The DbContext class for managing database operations related to cryptocurrency coins.
/// </summary>
public class CoinsDbContext(DbContextOptions<CoinsDbContext> options) : DbContext(options)
{
    public DbSet<CoinsEntity> Coins { get; set; } = null!;

    public DbSet<TradingPairsEntity> TradingPairs { get; set; } = null!;

    public DbSet<ExchangesEntity> Exchanges { get; set; } = null!;

    /// <summary>
    /// Configures the model and relationships using Fluent API.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureCoinEntity(modelBuilder);
        ConfigureTradingPairsExchanges(modelBuilder);
    }

    private static void ConfigureCoinEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CoinsEntity>(entity =>
        {
            // If IdCoinGecko is not null, it's value must be unique
            entity
                .HasIndex(c => c.IdCoinGecko)
                .HasFilter("[IdCoinGecko] IS NOT NULL")
                .IsUnique()
                .HasDatabaseName("UQ_Coins_IdCoinGecko");

            // Coin's symbol must always be uppercase
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Coins_Symbol_Uppercase", "[Symbol] = UPPER([Symbol])");
            });
        });
    }

    private static void ConfigureTradingPairsExchanges(ModelBuilder modelBuilder)
    {
        modelBuilder
            .Entity<TradingPairsEntity>()
            .HasMany(tp => tp.Exchanges)
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "TradingPairsExchanges",
                j => j.HasOne<ExchangesEntity>().WithMany().HasForeignKey("IdExchange"),
                j => j.HasOne<TradingPairsEntity>().WithMany().HasForeignKey("IdTradingPair"),
                j => j.HasKey("IdTradingPair", "IdExchange")
            );
    }
}
