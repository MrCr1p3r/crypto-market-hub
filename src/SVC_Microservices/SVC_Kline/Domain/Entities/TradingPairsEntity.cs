namespace SVC_Kline.Domain.Entities;

/// <summary>
/// Minimal or “stub” entity just to represent the TradingPairs table
/// so EF can keep the real foreign key in the schema.
/// </summary>
public class TradingPairsEntity
{
    /// <summary>
    /// The primary key in the trading pairs table.
    /// </summary>
    public int Id { get; set; }
}
