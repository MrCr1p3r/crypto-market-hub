using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace SVC_Coins.Models.Entities;

/// <summary>
/// Represents a cryptocurrency entity in the database.
/// </summary>
[PrimaryKey(nameof(Id))]
public class CoinEntity
{
    /// <summary>
    /// Unique identifier for the coin. 
    /// This is the primary key of the table.
    /// </summary>
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// Symbol of the cryptocurrency (e.g., "BTC" for Bitcoin).
    /// </summary>
    [Required]
    [StringLength(50)]
    public required string Symbol { get; set; }

    /// <summary>
    /// Full name of the cryptocurrency (e.g., "Bitcoin").
    /// </summary>
    [Required]
    [StringLength(50)]
    public required string Name { get; set; }

    /// <summary>
    /// Navigation property for trading pairs where this coin is the main coin.
    /// </summary>
    public ICollection<TradingPairEntity> TradingPairs { get; set; } = [];
}