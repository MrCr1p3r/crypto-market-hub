using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SVC_Coins.Models.Entities;

/// <summary>
/// Represents a trading pair in the database.
/// </summary>
[Table("TradingPairs")]
[PrimaryKey(nameof(Id))]
public class TradingPairEntity
{
    /// <summary>
    /// Unique identifier for the trading pair.
    /// </summary>
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// Foreign key referencing the main coin in the trading pair.
    /// </summary>
    [Required]
    public int IdCoinMain { get; set; }

    /// <summary>
    /// Foreign key referencing the quote coin in the trading pair.
    /// </summary>
    [Required]
    public int IdCoinQuote { get; set; }

    /// <summary>
    /// Navigation property for the main coin.
    /// </summary>
    [ForeignKey(nameof(IdCoinMain))]
    public CoinEntity CoinMain { get; set; } = null!;

    /// <summary>
    /// Navigation property for the quote coin.
    /// </summary>
    [ForeignKey(nameof(IdCoinQuote))]
    public CoinEntity CoinQuote { get; set; } = null!;
}
