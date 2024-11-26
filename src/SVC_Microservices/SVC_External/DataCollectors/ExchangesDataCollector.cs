using SVC_External.Models.Input;
using SVC_External.Models.Output;
using SVC_External.Clients.Interfaces;
using SVC_External.DataCollectors.Interfaces;

namespace SVC_External.DataCollectors;

/// <summary>
/// Implementation of the data collector that fetches the data from multiple exchange clients.
/// </summary>
public class ExchangesDataCollector(IEnumerable<IExchangeClient> exchangeClients) : IExchangesDataCollector
{
    private readonly IEnumerable<IExchangeClient> _exchangeClients = exchangeClients;

    /// <inheritdoc />
    public async Task<IEnumerable<KlineData>> GetKlineData(KlineDataRequest request)
    {
        var formattedRequest = Mapping.ToFormattedRequest(request);

        foreach (var client in _exchangeClients)
        {
            var result = await client.GetKlineData(formattedRequest);
            if (result.Any())
            {
                return result;
            }
        }

        return [];
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetAllListedCoins()
    {
        var coinLists = await Task.WhenAll(_exchangeClients.Select(client => client.GetAllListedCoins()));
        return coinLists.SelectMany(coins => coins).Distinct();
    }

    private static class Mapping
    {
        public static KlineDataRequestFormatted ToFormattedRequest(KlineDataRequest request) => new()
        {
            CoinMain = request.CoinMain,
            CoinQuote = request.CoinQuote,
            Interval = request.Interval,
            StartTimeUnix = new DateTimeOffset(request.StartTime).ToUnixTimeMilliseconds(),
            EndTimeUnix = new DateTimeOffset(request.EndTime).ToUnixTimeMilliseconds(),
            Limit = request.Limit
        };
    }
}
