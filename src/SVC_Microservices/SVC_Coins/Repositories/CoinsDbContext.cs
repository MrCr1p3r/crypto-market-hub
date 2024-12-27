using Microsoft.EntityFrameworkCore;
using SVC_Coins.Models.Entities;

namespace SVC_Coins.Repositories;

/// <summary>
/// The DbContext class for managing database operations related to cryptocurrency coins.
/// </summary>
public class CoinsDbContext(DbContextOptions<CoinsDbContext> options) : DbContext(options)
{
    public DbSet<CoinsEntity> Coins { get; set; } = null!;
    public DbSet<TradingPairsEntity> TradingPairs { get; set; } = null!;

    /// <summary>
    /// Configures the model and relationships using Fluent API.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureCoinEntity(modelBuilder);
        ConfigureTradingPairEntity(modelBuilder);
    }

    private static void ConfigureCoinEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CoinsEntity>(entity =>
        {
            entity.HasKey(c => c.Id).HasName("PK_Coins");
            entity
                .Property(c => c.Symbol)
                .HasMaxLength(50)
                .IsUnicode(true)
                .UseCollation("Latin1_General_CS_AS");
            entity.Property(c => c.Name).HasMaxLength(50).IsUnicode(true);
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Coins_Symbol_Uppercase", "[Symbol] = UPPER([Symbol])");
            });
            entity
                .HasIndex(c => new { c.Symbol, c.Name })
                .IsUnique()
                .HasDatabaseName("UQ_Coins_Symbol_Name");
            entity
                .HasMany(c => c.TradingPairs)
                .WithOne(tp => tp.CoinMain)
                .HasForeignKey(tp => tp.IdCoinMain)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    private static void ConfigureTradingPairEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TradingPairsEntity>(entity =>
        {
            entity
                .HasIndex(e => new { e.IdCoinMain, e.IdCoinQuote })
                .IsUnique()
                .HasDatabaseName("UQ_TradingPairs_IdCoinMain_IdCoinQuote");
            entity
                .HasOne(tp => tp.CoinQuote)
                .WithMany()
                .HasForeignKey(tp => tp.IdCoinQuote)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }
}
