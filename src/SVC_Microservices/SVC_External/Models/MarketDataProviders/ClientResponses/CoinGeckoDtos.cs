using System.Text.Json.Serialization;

namespace SVC_External.Models.MarketDataProviders.ClientResponses;

public static class CoinGeckoDtos
{
    public class CoinListResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("symbol")]
        public string Symbol { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    public class ExchangeTickersResponse
    {
        [JsonPropertyName("tickers")]
        public IEnumerable<TickerData> Tickers { get; set; } = [];
    }

    public class TickerData
    {
        [JsonPropertyName("base")]
        public string BaseCoin { get; set; } = string.Empty;

        [JsonPropertyName("target")]
        public string TargetCoin { get; set; } = string.Empty;

        [JsonPropertyName("coin_id")]
        public string BaseCoinId { get; set; } = string.Empty;

        [JsonPropertyName("target_coin_id")]
        public string TargetCoinId { get; set; } = string.Empty;
    }
}
