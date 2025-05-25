using System.Text.Json.Serialization;

namespace SVC_External.ExternalClients.Exchanges.Binance;

/// <summary>
/// Represents the data transfer objects for the Binance API.
/// </summary>
public static class BinanceDtos
{
    /// <summary>
    /// Represents the response from the Binance API.
    /// </summary>
    public class Response
    {
        /// <summary>
        /// The trading pairs.
        /// </summary>
        [JsonPropertyName("symbols")]
        public required HashSet<TradingPair> TradingPairs { get; set; }
    }

    /// <summary>
    /// Represents a trading pair on Binance.
    /// </summary>
    public record TradingPair
    {
        /// <summary>
        /// The base asset symbol.
        /// </summary>
        [JsonPropertyName("baseAsset")]
        public required string BaseAssetSymbol { get; set; }

        /// <summary>
        /// The quote asset symbol.
        /// </summary>
        [JsonPropertyName("quoteAsset")]
        public required string QuoteAssetSymbol { get; set; }

        /// <summary>
        /// The status of the trading pair.
        /// </summary>
        [JsonPropertyName("status")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public required TradingPairStatus Status { get; set; }
    }

    public enum TradingPairStatus
    {
        /// <summary>
        /// Trading pair is actively trading.
        /// </summary>
        TRADING,

        /// <summary>
        /// Trading pair is temporarily halted.
        /// </summary>
        HALT,

        /// <summary>
        /// Trading pair is on break.
        /// </summary>
        BREAK,
    }
}
