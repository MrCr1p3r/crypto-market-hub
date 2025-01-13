using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SVC_Kline.Models.Entities;

/// <summary>
/// Entity class representing Kline (candlestick) data for a trading pair in the database.
/// </summary>
[PrimaryKey(nameof(IdTradePair), nameof(OpenTime))]
public class KlineDataEntity
{
    /// <summary>
    /// Id of the trade pair for which the Kline data is recorded.
    /// </summary>
    [Required]
    public int IdTradePair { get; set; }

    /// <summary>
    /// The opening time of the Kline in milliseconds since the Unix epoch.
    /// </summary>
    [Required]
    public long OpenTime { get; set; }

    /// <summary>
    /// The opening price at the start of the Kline period.
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal OpenPrice { get; set; }

    /// <summary>
    /// The highest price during the Kline period.
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal HighPrice { get; set; }

    /// <summary>
    /// The lowest price during the Kline period.
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal LowPrice { get; set; }

    /// <summary>
    /// The closing price at the end of the Kline period.
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal ClosePrice { get; set; }

    /// <summary>
    /// The total volume traded during the Kline period.
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Volume { get; set; }

    /// <summary>
    /// The closing time of the Kline in milliseconds since the Unix epoch.
    /// </summary>
    [Required]
    public long CloseTime { get; set; }

    /// <summary>
    /// The trading pair for which the Kline data is recorded.
    /// </summary>
    public TradingPairEntity IdTradePairNavigation { get; set; } = null!;
}
