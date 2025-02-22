using System.Text.Json.Serialization;

namespace SVC_External.Models.Exchanges.ClientResponses;

public static class BinanceDtos
{
    public class Response
    {
        [JsonPropertyName("symbols")]
        public required HashSet<TradingPair> TradingPairs { get; set; }
    }

    public record TradingPair
    {
        [JsonPropertyName("baseAsset")]
        public required string BaseAssetSymbol { get; set; }

        [JsonPropertyName("quoteAsset")]
        public required string QuoteAssetSymbol { get; set; }

        [JsonPropertyName("status")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public required TradingPairStatus Status { get; set; }
    }

    public enum TradingPairStatus
    {
        TRADING,
        HALT,
        BREAK,
    }
}
