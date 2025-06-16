using System.Text.Json.Serialization;

namespace SVC_External.ExternalClients.MarketDataProviders.CoinGecko;

/// <summary>
/// Represents the data transfer objects for the CoinGecko API.
/// </summary>
public static class CoinGeckoDtos
{
    /// <summary>
    /// Represents a coin entry from the CoinGecko coin list API response.
    /// </summary>
    public class CoinListResponse
    {
        /// <summary>
        /// The unique identifier for the coin on CoinGecko.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// The symbol/ticker of the coin (e.g., "BTC", "ETH").
        /// </summary>
        [JsonPropertyName("symbol")]
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// The full name of the coin (e.g., "Bitcoin", "Ethereum").
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents the response from the CoinGecko exchange tickers API.
    /// </summary>
    public class ExchangeTickersResponse
    {
        /// <summary>
        /// The collection of ticker data for trading pairs on the exchange.
        /// </summary>
        [JsonPropertyName("tickers")]
        public IEnumerable<TickerData> Tickers { get; set; } = [];
    }

    /// <summary>
    /// Represents ticker data for a specific trading pair on an exchange.
    /// </summary>
    public class TickerData
    {
        /// <summary>
        /// The base coin symbol in the trading pair (e.g., "BTC" in BTC/USDT).
        /// </summary>
        [JsonPropertyName("base")]
        public string BaseCoin { get; set; } = string.Empty;

        /// <summary>
        /// The target/quote coin symbol in the trading pair (e.g., "USDT" in BTC/USDT).
        /// </summary>
        [JsonPropertyName("target")]
        public string TargetCoin { get; set; } = string.Empty;

        /// <summary>
        /// The CoinGecko ID for the base coin.
        /// </summary>
        [JsonPropertyName("coin_id")]
        public string BaseCoinId { get; set; } = string.Empty;

        /// <summary>
        /// The CoinGecko ID for the target/quote coin.
        /// </summary>
        [JsonPropertyName("target_coin_id")]
        public string TargetCoinId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents an error response from CoinGecko API.
    /// </summary>
    public class CoinGeckoErrorResponse
    {
        /// <summary>
        /// Status information about the error.
        /// </summary>
        [JsonPropertyName("status")]
        public CoinGeckoErrorStatus? Status { get; set; }
    }

    /// <summary>
    /// Represents the status section of a CoinGecko error response.
    /// </summary>
    public class CoinGeckoErrorStatus
    {
        /// <summary>
        /// Timestamp when the error occurred.
        /// </summary>
        [JsonPropertyName("timestamp")]
        public string? Timestamp { get; set; }

        /// <summary>
        /// Error code from CoinGecko API.
        /// </summary>
        [JsonPropertyName("error_code")]
        public int ErrorCode { get; set; }

        /// <summary>
        /// Error message from CoinGecko API.
        /// </summary>
        [JsonPropertyName("error_message")]
        public string? ErrorMessage { get; set; }
    }
}
