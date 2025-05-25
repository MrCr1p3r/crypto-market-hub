using System.Text.Json.Serialization;

namespace SVC_External.ExternalClients.Exchanges.Bybit;

/// <summary>
/// Represents the data transfer objects for the Bybit API.
/// </summary>
public static class BybitDtos
{
    /// <summary>
    /// Base class for all Bybit API responses containing common response properties.
    /// </summary>
    public class BybitResponseBase
    {
        /// <summary>
        /// The response code indicating success (0) or error (non-zero).
        /// </summary>
        [JsonPropertyName("retCode")]
        public int ResponseCode { get; set; }

        /// <summary>
        /// The response message providing additional information about the result.
        /// </summary>
        [JsonPropertyName("retMsg")]
        public string ResponseMessage { get; set; } = string.Empty;
    }

    #region Kline

    /// <summary>
    /// Represents the response from the Bybit kline (candlestick) data API.
    /// </summary>
    public class BybitKlineResponse : BybitResponseBase
    {
        /// <summary>
        /// The kline data result containing the list of candlestick data.
        /// </summary>
        [JsonPropertyName("result")]
        public BybitKlineResult Result { get; set; } = new();
    }

    /// <summary>
    /// Contains the kline data result from the Bybit API.
    /// </summary>
    public class BybitKlineResult
    {
        /// <summary>
        /// List of kline data arrays, where each inner list contains [timestamp, open, high, low, close, volume].
        /// </summary>
        [JsonPropertyName("list")]
        public List<List<string>> List { get; set; } = [];
    }
    #endregion

    #region SpotAssets

    /// <summary>
    /// Represents the response from the Bybit spot assets (instruments info) API.
    /// </summary>
    public class BybitSpotAssetsResponse : BybitResponseBase
    {
        /// <summary>
        /// The spot assets result containing the list of trading pairs.
        /// </summary>
        [JsonPropertyName("result")]
        public BybitSpotAssetsResult Result { get; set; } = new();
    }

    /// <summary>
    /// Contains the spot assets result from the Bybit API.
    /// </summary>
    public class BybitSpotAssetsResult
    {
        /// <summary>
        /// The collection of trading pairs available on Bybit.
        /// </summary>
        [JsonPropertyName("list")]
        public HashSet<TradingPair> TradingPairs { get; set; } = [];
    }

    /// <summary>
    /// Represents a trading pair on Bybit.
    /// </summary>
    public record TradingPair
    {
        /// <summary>
        /// The base asset symbol (e.g., "BTC" in BTC/USDT).
        /// </summary>
        [JsonPropertyName("baseCoin")]
        public required string BaseAssetSymbol { get; set; }

        /// <summary>
        /// The quote asset symbol (e.g., "USDT" in BTC/USDT).
        /// </summary>
        [JsonPropertyName("quoteCoin")]
        public required string QuoteAssetSymbol { get; set; }

        /// <summary>
        /// The current trading status of the trading pair.
        /// </summary>
        [JsonPropertyName("status")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public required TradingPairStatus TradingStatus { get; set; }
    }

    /// <summary>
    /// Represents the possible trading statuses for a trading pair on Bybit.
    /// </summary>
    public enum TradingPairStatus
    {
        /// <summary>
        /// Trading pair is actively trading.
        /// </summary>
        Trading,

        /// <summary>
        /// Trading pair is in pre-launch phase and not yet available for trading.
        /// </summary>
        PreLaunch,

        /// <summary>
        /// Trading pair is in settling phase.
        /// </summary>
        Settling,

        /// <summary>
        /// Trading pair is in delivery phase.
        /// </summary>
        Delivering,

        /// <summary>
        /// Trading pair is closed and not available for trading.
        /// </summary>
        Closed,
    }
    #endregion
}
