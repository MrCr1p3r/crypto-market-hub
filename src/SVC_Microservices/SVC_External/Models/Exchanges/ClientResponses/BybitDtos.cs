using System.Text.Json.Serialization;

namespace SVC_External.Models.Exchanges.ClientResponses;

public static class BybitDtos
{
    public class BybitKlineResponse
    {
        [JsonPropertyName("result")]
        public BybitKlineResult Result { get; set; } = new();
    }

    public class BybitKlineResult
    {
        [JsonPropertyName("list")]
        public List<List<string>> List { get; set; } = [];
    }

    public class BybitSpotAssetsResponse
    {
        [JsonPropertyName("result")]
        public BybitSpotAssetsResult Result { get; set; } = new();
    }

    public class BybitSpotAssetsResult
    {
        [JsonPropertyName("list")]
        public HashSet<TradingPair> TradingPairs { get; set; } = [];
    }

    public record TradingPair
    {
        [JsonPropertyName("baseCoin")]
        public required string BaseAssetSymbol { get; set; }

        [JsonPropertyName("quoteCoin")]
        public required string QuoteAssetSymbol { get; set; }

        [JsonPropertyName("status")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public required TradingPairStatus TradingStatus { get; set; }
    }

    public enum TradingPairStatus
    {
        Trading,
        PreLaunch,
        Settling,
        Delivering,
        Closed,
    }
}
