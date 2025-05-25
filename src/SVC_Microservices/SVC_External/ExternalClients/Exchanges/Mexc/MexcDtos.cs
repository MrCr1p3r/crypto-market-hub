using System.Text.Json.Serialization;

namespace SVC_External.ExternalClients.Exchanges.Mexc;

/// <summary>
/// Represents the data transfer objects for the MEXC API.
/// </summary>
public static class MexcDtos
{
    /// <summary>
    /// Represents the response from the MEXC exchange info API.
    /// </summary>
    public class Response
    {
        /// <summary>
        /// The collection of trading pairs available on MEXC.
        /// </summary>
        [JsonPropertyName("symbols")]
        public required HashSet<TradingPair> TradingPairs { get; set; }
    }

    /// <summary>
    /// Represents a trading pair on the MEXC exchange.
    /// </summary>
    public record TradingPair
    {
        /// <summary>
        /// The base asset symbol (e.g., "BTC" in BTC/USDT).
        /// </summary>
        [JsonPropertyName("baseAsset")]
        public required string BaseAssetSymbol { get; set; }

        /// <summary>
        /// The quote asset symbol (e.g., "USDT" in BTC/USDT).
        /// </summary>
        [JsonPropertyName("quoteAsset")]
        public required string QuoteAssetSymbol { get; set; }

        /// <summary>
        /// The current trading status of the trading pair.
        /// </summary>
        [JsonPropertyName("status")]
        [JsonConverter(typeof(JsonNumberEnumConverter<TradingPairStatus>))]
        public required TradingPairStatus Status { get; set; }

        /// <summary>
        /// The full name of the base asset.
        /// </summary>
        [JsonPropertyName("fullName")]
        public required string BaseAssetFullName { get; set; }
    }

    /// <summary>
    /// Represents the trading status of a trading pair on MEXC.
    /// </summary>
    public enum TradingPairStatus
    {
        /// <summary>
        /// Trading pair is actively trading.
        /// </summary>
        Trading = 1,

        /// <summary>
        /// Trading pair is currently unavailable but may resume.
        /// </summary>
        CurrentlyUnavailable = 2,

        /// <summary>
        /// Trading pair is permanently unavailable.
        /// </summary>
        Unavailable = 3,
    }
}
