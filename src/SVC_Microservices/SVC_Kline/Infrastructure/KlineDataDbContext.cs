using Microsoft.EntityFrameworkCore;
using SVC_Kline.Domain.Entities;

namespace SVC_Kline.Infrastructure;

/// <summary>
/// The DbContext class for managing database operations related to Kline data.
/// </summary>
/// <param name="options">The options to configure the DbContext.</param>
public class KlineDataDbContext(DbContextOptions<KlineDataDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Gets or sets the DbSet for Kline data entities.
    /// </summary>
    public DbSet<KlineDataEntity> KlineData { get; set; } = null!;

    /// <summary>
    /// Gets or sets the DbSet for trading pair entities.
    /// </summary>
    public DbSet<TradingPairsEntity> TradingPair { get; set; } = null!;

    /// <summary>
    /// Configures the model and relationships using Fluent API.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<KlineDataEntity>(entity =>
        {
            entity
                .HasKey(e => new { e.IdTradingPair, e.OpenTime })
                .HasName("PK__KlineDat__031B42F9984E192C");

            entity.Property(e => e.ClosePrice).HasMaxLength(100).IsUnicode(true);
            entity.Property(e => e.HighPrice).HasMaxLength(100).IsUnicode(true);
            entity.Property(e => e.LowPrice).HasMaxLength(100).IsUnicode(true);
            entity.Property(e => e.OpenPrice).HasMaxLength(100).IsUnicode(true);
            entity.Property(e => e.Volume).HasMaxLength(100).IsUnicode(true);

            entity
                .HasOne(d => d.IdTradePairNavigation)
                .WithMany()
                .HasForeignKey(d => d.IdTradingPair)
                .HasConstraintName("FK_KlineData_TradingPairs");
        });

        modelBuilder.Entity<TradingPairsEntity>().ToTable("TradingPairs").HasKey(e => e.Id);
    }
}
