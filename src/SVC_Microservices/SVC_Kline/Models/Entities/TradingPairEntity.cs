namespace SVC_Kline.Models.Entities;

/// <summary>
/// Minimal or “stub” entity just to represent the TradingPair table
/// so EF can keep the real foreign key in the schema.
/// </summary>
public class TradingPairEntity
{
    /// <summary>
    /// The primary key in the trading pairs table
    /// </summary>
    public int Id { get; set; }
}
