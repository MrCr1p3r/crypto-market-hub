using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace SVC_Coins.Models.Entities;

/// <summary>
/// Represents a cryptocurrency entity in the database.
/// </summary>
[PrimaryKey(nameof(Id))]
public class CoinsEntity
{
    /// <summary>
    /// Unique identifier for the coin.
    /// This is the primary key of the table.
    /// </summary>
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// Symbol of the cryptocurrency (e.g., "BTC" for Bitcoin).
    /// Lowercase letters will be automatically converted to uppercase.
    /// </summary>
    [Required]
    [StringLength(50)]
    public required string Symbol
    {
        get => _symbol;
        set => _symbol = value.ToUpper();
    }
    private string _symbol = string.Empty;

    /// <summary>
    /// Full name of the cryptocurrency (e.g., "Bitcoin").
    /// </summary>
    [Required]
    [StringLength(50)]
    public required string Name { get; set; }

    /// <summary>
    /// Determines sorting order for quote coins.
    /// Null means that the coin is not a quote coin.
    /// </summary>
    public int? QuoteCoinPriority { get; set; }

    /// <summary>
    /// Indicates if the coin is a stablecoin.
    /// </summary>
    [Required]
    public bool IsStablecoin { get; set; }

    /// <summary>
    /// Navigation property for trading pairs where this coin is the main coin.
    /// </summary>
    public ICollection<TradingPairsEntity> TradingPairs { get; set; } = [];
}
