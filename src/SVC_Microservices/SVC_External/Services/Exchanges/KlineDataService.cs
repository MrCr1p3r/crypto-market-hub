using FluentResults;
using SVC_External.ApiContracts.Requests;
using SVC_External.ApiContracts.Responses.Exchanges.KlineData;
using SVC_External.ExternalClients.Exchanges;
using SVC_External.ExternalClients.Exchanges.Contracts.Requests;
using SVC_External.ExternalClients.Exchanges.Contracts.Responses;
using SVC_External.Services.Exchanges.Interfaces;
using SVC_External.Services.Exchanges.Logging;
using static SharedLibrary.Errors.GenericErrors;

namespace SVC_External.Services.Exchanges;

/// <summary>
/// Implementation of the kline data service that fetches kline data from multiple exchange clients.
/// </summary>
public class KlineDataService(
    IEnumerable<IExchangesClient> exchangeClients,
    ILogger<KlineDataService> logger
) : IKlineDataService
{
    private readonly IEnumerable<IExchangesClient> _exchangeClients = exchangeClients;
    private readonly ILogger<KlineDataService> _logger = logger;

    /// <inheritdoc />
    public async Task<Result<KlineDataResponse>> GetKlineDataForTradingPair(
        KlineDataRequest request
    )
    {
        var suitableClients = PickExchangesClientsForTradingPair(request.TradingPair);
        var formattedRequest = Mapping.ToFormattedRequest(request);
        var klineData = await GetKlineDataForTradingPair(suitableClients, formattedRequest);
        if (!klineData.Any())
        {
            KlineDataServiceLogging.LogNoKlineDataFoundForCoin(
                _logger,
                request.CoinMain.Id,
                request.CoinMain.Symbol,
                request.CoinMain.Name
            );
            return Result.Fail(
                new InternalError(
                    $"No kline data found for trading pair with ID: {request.TradingPair.Id}"
                )
            );
        }

        var output = Mapping.ToOutputKlineDataRequestResponse(request.TradingPair.Id, klineData);
        return Result.Ok(output);
    }

    private IEnumerable<IExchangesClient> PickExchangesClientsForTradingPair(
        KlineDataRequestTradingPair tradingPair
    ) =>
        tradingPair.Exchanges.Select(exchange =>
        {
            return _exchangeClients.First(client => client.CurrentExchange == exchange);
        });

    private static async Task<IEnumerable<ExchangeKlineData>> GetKlineDataForTradingPair(
        IEnumerable<IExchangesClient> suitableClients,
        ExchangeKlineDataRequest formattedRequest
    )
    {
        foreach (var client in suitableClients)
        {
            var result = await client.GetKlineData(formattedRequest);
            if (result.IsSuccess && result.Value.Any())
            {
                return result.Value;
            }
        }

        return [];
    }

    /// <inheritdoc />
    public async Task<IEnumerable<KlineDataResponse>> GetFirstSuccessfulKlineDataPerCoin(
        KlineDataBatchRequest request
    )
    {
        var coinTasks = request.MainCoins.Select(mainCoin =>
            ProcessKlineDataRequest(request, mainCoin)
        );
        var results = await Task.WhenAll(coinTasks);

        var responses = new List<KlineDataResponse>();
        foreach (var result in results.Where(result => result.IsSuccess))
        {
            var response = new KlineDataResponse
            {
                IdTradingPair = result.Value.Key,
                KlineData = result.Value.Value,
            };
            responses.Add(response);
        }

        return responses;
    }

    private async Task<Result<KeyValuePair<int, IEnumerable<KlineData>>>> ProcessKlineDataRequest(
        KlineDataBatchRequest request,
        KlineDataRequestCoinMain mainCoin
    )
    {
        foreach (var tradingPair in mainCoin.TradingPairs)
        {
            var suitableClients = PickExchangesClientsForTradingPair(tradingPair);
            var formattedRequest = Mapping.ToFormattedRequest(
                mainCoin.Symbol,
                tradingPair.CoinQuote.Symbol,
                request
            );
            var klineData = await GetKlineDataForTradingPair(suitableClients, formattedRequest);
            if (klineData.Any())
            {
                var klineDataOutput = klineData.Select(Mapping.ToOutputKlineData);
                var kvp = new KeyValuePair<int, IEnumerable<KlineData>>(
                    tradingPair.Id,
                    klineDataOutput
                );
                return Result.Ok(kvp);
            }
        }

        KlineDataServiceLogging.LogNoKlineDataFoundForCoin(
            _logger,
            mainCoin.Id,
            mainCoin.Symbol,
            mainCoin.Name
        );
        return Result.Fail(
            new InternalError($"No kline data found for coin with ID: {mainCoin.Id}.")
        );
    }

    private static class Mapping
    {
        public static ExchangeKlineDataRequest ToFormattedRequest(KlineDataRequest request) =>
            new()
            {
                CoinMainSymbol = request.CoinMain.Symbol,
                CoinQuoteSymbol = request.TradingPair.CoinQuote.Symbol,
                Interval = request.Interval,
                StartTimeUnix = new DateTimeOffset(request.StartTime).ToUnixTimeMilliseconds(),
                EndTimeUnix = new DateTimeOffset(request.EndTime).ToUnixTimeMilliseconds(),
                Limit = request.Limit,
            };

        public static ExchangeKlineDataRequest ToFormattedRequest(
            string mainCoinSymbol,
            string quoteCoinSymbol,
            KlineDataBatchRequest request
        ) =>
            new()
            {
                CoinMainSymbol = mainCoinSymbol,
                CoinQuoteSymbol = quoteCoinSymbol,
                Interval = request.Interval,
                StartTimeUnix = new DateTimeOffset(request.StartTime).ToUnixTimeMilliseconds(),
                EndTimeUnix = new DateTimeOffset(request.EndTime).ToUnixTimeMilliseconds(),
                Limit = request.Limit,
            };

        public static KlineDataResponse ToOutputKlineDataRequestResponse(
            int idTradingPair,
            IEnumerable<ExchangeKlineData> klineData
        ) =>
            new()
            {
                IdTradingPair = idTradingPair,
                KlineData = klineData.Select(ToOutputKlineData),
            };

        public static KlineData ToOutputKlineData(ExchangeKlineData klineData) =>
            new()
            {
                OpenTime = klineData.OpenTime,
                OpenPrice = klineData.OpenPrice,
                HighPrice = klineData.HighPrice,
                LowPrice = klineData.LowPrice,
                ClosePrice = klineData.ClosePrice,
                Volume = klineData.Volume,
                CloseTime = klineData.CloseTime,
            };
    }
}
