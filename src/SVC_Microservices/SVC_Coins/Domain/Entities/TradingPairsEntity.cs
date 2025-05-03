using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SVC_Coins.Domain.Entities;

/// <summary>
/// Represents a trading pair in the database.
/// </summary>
[Table("TradingPairs")]
[Index(
    nameof(IdCoinMain),
    nameof(IdCoinQuote),
    IsUnique = true,
    Name = "UQ_TradingPairs_IdCoinMain_IdCoinQuote"
)]
public class TradingPairsEntity
{
    /// <summary>
    /// Gets or sets unique identifier for the trading pair.
    /// </summary>
    [Required]
    public int Id { get; init; }

    /// <summary>
    /// Gets or sets foreign key referencing the main coin in the trading pair.
    /// </summary>
    [Required]
    public required int IdCoinMain { get; init; }

    /// <summary>
    /// Gets or sets navigation property for the main coin.
    /// </summary>
    [ForeignKey(nameof(IdCoinMain))]
    [DeleteBehavior(DeleteBehavior.NoAction)]
    [InverseProperty(nameof(CoinsEntity.TradingPairs))]
    public CoinsEntity CoinMain { get; init; } = null!;

    /// <summary>
    /// Gets or sets foreign key referencing the quote coin in the trading pair.
    /// </summary>
    [Required]
    public required int IdCoinQuote { get; init; }

    /// <summary>
    /// Gets or sets navigation property for the quote coin.
    /// </summary>
    [ForeignKey(nameof(IdCoinQuote))]
    [DeleteBehavior(DeleteBehavior.NoAction)]
    public CoinsEntity CoinQuote { get; init; } = null!;

    /// <summary>
    /// Gets or sets navigation property for the exchanges.
    /// </summary>
    [Required]
    public ICollection<ExchangesEntity> Exchanges { get; init; } = [];
}
