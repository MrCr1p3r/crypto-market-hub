using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SVC_External.Models.Exchanges.ClientResponses;

public static class MexcDtos
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
        [JsonConverter(typeof(MexcStatusConverter))]
        public required TradingPairStatus Status { get; set; }

        [JsonPropertyName("fullName")]
        public required string BaseAssetFullName { get; set; }
    }

    public enum TradingPairStatus
    {
        Trading = 1,
        CurrentlyUnavailable = 2,
        Unavailable = 3,
    }

    public class MexcStatusConverter : JsonConverter<TradingPairStatus> // TODO: find a better way to do this
    {
        public override TradingPairStatus Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            var statusString = reader.GetString();
            return statusString switch
            {
                "1" => TradingPairStatus.Trading,
                "2" => TradingPairStatus.CurrentlyUnavailable,
                "3" => TradingPairStatus.Unavailable,
                _ => throw new JsonException($"Unsupported status value: {statusString}"),
            };
        }

        public override void Write(
            Utf8JsonWriter writer,
            TradingPairStatus value,
            JsonSerializerOptions options
        )
        {
            writer.WriteStringValue(((int)value).ToString(CultureInfo.InvariantCulture));
        }
    }
}
