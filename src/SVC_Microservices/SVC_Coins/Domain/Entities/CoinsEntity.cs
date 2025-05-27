using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SVC_Coins.Domain.Entities;

/// <summary>
/// Represents a cryptocurrency entity in the database.
/// </summary>
[Index(nameof(Symbol), nameof(Name), IsUnique = true, Name = "UQ_Coins_Symbol_Name")]
public class CoinsEntity
{
    /// <summary>
    /// Gets or sets unique identifier for the coin.
    /// This is the primary key of the table.
    /// </summary>
    [Required]
    public int Id { get; init; }

    /// <summary>
    /// Gets or sets symbol of the cryptocurrency (e.g., "BTC" for Bitcoin).
    /// </summary>
    [Required]
    [MaxLength(50)]
    [Unicode]
    public required string Symbol { get; init; }

    /// <summary>
    /// Gets or sets full name of the cryptocurrency (e.g., "Bitcoin").
    /// </summary>
    [Required]
    [MaxLength(50)]
    [Unicode]
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether indicates if the coin is a fiat currency.
    /// </summary>
    [Required]
    public bool IsFiat { get; init; }

    /// <summary>
    /// Gets or sets the ID of the coin in the CoinGecko API.
    /// </summary>
    [MaxLength(100)]
    public string? IdCoinGecko { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether indicates if the coin is a stablecoin.
    /// </summary>
    [Required]
    public bool IsStablecoin { get; init; }

    /// <summary>
    /// The market capitalization in USD.
    /// </summary>
    public long? MarketCapUsd { get; set; }

    /// <summary>
    /// The current price in USD.
    /// </summary>
    [MaxLength(100)]
    [Unicode(false)]
    public string? PriceUsd { get; set; }

    /// <summary>
    /// The price change in percentage over the last 24 hours.
    /// </summary>
    [Precision(22, 2)]
    public decimal? PriceChangePercentage24h { get; set; }

    /// <summary>
    /// Gets or sets navigation property for trading pairs where this coin is the main coin.
    /// </summary>
    [InverseProperty(nameof(TradingPairsEntity.CoinMain))]
    public ICollection<TradingPairsEntity> TradingPairs { get; init; } = [];
}
