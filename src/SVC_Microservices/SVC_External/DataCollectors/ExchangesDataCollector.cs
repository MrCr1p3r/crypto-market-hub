using SVC_External.Clients.Interfaces;
using SVC_External.DataCollectors.Interfaces;
using SVC_External.Models.Input;
using SVC_External.Models.Output;

namespace SVC_External.DataCollectors;

/// <summary>
/// Implementation of the data collector that fetches the data from multiple exchange clients.
/// </summary>
public class ExchangesDataCollector(IEnumerable<IExchangeClient> exchangeClients)
    : IExchangesDataCollector
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
    public async Task<ListedCoins> GetAllListedCoins()
    {
        var listedCoins = new ListedCoins();
        var coinLists = await Task.WhenAll(
            _exchangeClients.Select(client => client.GetAllListedCoins(listedCoins))
        );
        return listedCoins;
    }

    private static class Mapping
    {
        public static KlineDataRequestFormatted ToFormattedRequest(KlineDataRequest request) =>
            new()
            {
                CoinMain = request.CoinMain,
                CoinQuote = request.CoinQuote,
                Interval = request.Interval,
                StartTimeUnix = new DateTimeOffset(request.StartTime).ToUnixTimeMilliseconds(),
                EndTimeUnix = new DateTimeOffset(request.EndTime).ToUnixTimeMilliseconds(),
                Limit = request.Limit,
            };
    }
}
