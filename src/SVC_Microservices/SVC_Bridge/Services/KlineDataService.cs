using FluentResults;
using SharedLibrary.Enums;
using SVC_Bridge.ApiContracts.Responses.KlineData;
using SVC_Bridge.MicroserviceClients.SvcCoins;
using SVC_Bridge.MicroserviceClients.SvcCoins.Contracts.Responses;
using SVC_Bridge.MicroserviceClients.SvcExternal;
using SVC_Bridge.MicroserviceClients.SvcExternal.Contracts.Requests;
using SVC_Bridge.MicroserviceClients.SvcKline;
using SVC_Bridge.MicroserviceClients.SvcKline.Contracts.Requests;
using SVC_Bridge.Services.Interfaces;
using static SharedLibrary.Errors.GenericErrors;
using SvcExternal = SVC_Bridge.MicroserviceClients.SvcExternal.Contracts.Responses;
using SvcKline = SVC_Bridge.MicroserviceClients.SvcKline.Contracts.Responses;

namespace SVC_Bridge.Services;

/// <summary>
/// Service for managing kline data operations.
/// </summary>
public class KlineDataService(
    ISvcCoinsClient svcCoinsClient,
    ISvcExternalClient svcExternalClient,
    ISvcKlineClient svcKlineClient
) : IKlineDataService
{
    private readonly ISvcCoinsClient _svcCoinsClient = svcCoinsClient;
    private readonly ISvcExternalClient _svcExternalClient = svcExternalClient;
    private readonly ISvcKlineClient _svcKlineClient = svcKlineClient;

    /// <inheritdoc />
    public async Task<Result<IEnumerable<KlineDataResponse>>> UpdateKlineData()
    {
        // Step 1: Get all coins from the coins service
        var coinsResult = await _svcCoinsClient.GetAllCoins();
        if (coinsResult.IsFailed)
        {
            return Result.Fail(
                new InternalError(
                    "Failed to retrieve coins from coins service.",
                    reasons: coinsResult.Errors
                )
            );
        }

        var coins = coinsResult.Value;

        // Step 2: Map coins to kline data requests
        var klineDataRequest = Mapping.ToKlineDataBatchRequest(coins);
        if (!klineDataRequest.MainCoins.Any())
        {
            return Result.Ok(Enumerable.Empty<KlineDataResponse>());
        }

        // Step 3: Get new kline data from SVC_External
        var klineDataResult = await _svcExternalClient.GetKlineData(klineDataRequest);
        if (klineDataResult.IsFailed)
        {
            return Result.Fail(
                new InternalError(
                    "Failed to retrieve kline data from external service.",
                    reasons: klineDataResult.Errors
                )
            );
        }

        // Step 4: Map new kline data to kline data creation requests
        var klineDataCreationRequests = Mapping.ToKlineDataCreationRequests(klineDataResult.Value);
        if (!klineDataCreationRequests.Any())
        {
            return Result.Ok(Enumerable.Empty<KlineDataResponse>());
        }

        // Step 5: Replace old kline data with new one using SVC_Kline
        var replaceResult = await _svcKlineClient.ReplaceKlineData(klineDataCreationRequests);
        if (replaceResult.IsFailed)
        {
            return Result.Fail(
                new InternalError("Failed to replace kline data.", reasons: replaceResult.Errors)
            );
        }

        // Step 6: Map freshly inserted kline data to response format and return
        var responseData = Mapping.ToApiContractKlineDataResponse(replaceResult.Value);
        return Result.Ok(responseData);
    }

    private static class Mapping
    {
        /// <summary>
        /// Maps coins to a kline data batch request.
        /// </summary>
        public static KlineDataBatchRequest ToKlineDataBatchRequest(IEnumerable<Coin> coins)
        {
            var coinsWithTradingPairs = coins.Where(coin => coin.TradingPairs.Any());

            return new KlineDataBatchRequest
            {
                Interval = ExchangeKlineInterval.OneDay, // Default to daily data
                StartTime = DateTime.UtcNow.AddDays(-30), // Last 30 days
                EndTime = DateTime.UtcNow,
                Limit = 1000,
                MainCoins = coinsWithTradingPairs.Select(coin => new KlineDataRequestCoinMain
                {
                    Id = coin.Id,
                    Symbol = coin.Symbol,
                    Name = coin.Name,
                    TradingPairs = coin.TradingPairs.Select(
                        tradingPair => new KlineDataRequestTradingPair
                        {
                            Id = tradingPair.Id,
                            CoinQuote = new KlineDataRequestCoinQuote
                            {
                                Id = tradingPair.CoinQuote.Id,
                                Symbol = tradingPair.CoinQuote.Symbol,
                                Name = tradingPair.CoinQuote.Name,
                            },
                            Exchanges = tradingPair.Exchanges,
                        }
                    ),
                }),
            };
        }

        /// <summary>
        /// Maps kline data responses to kline data creation requests.
        /// </summary>
        public static IEnumerable<KlineDataCreationRequest> ToKlineDataCreationRequests(
            IEnumerable<SvcExternal.KlineDataResponse> klineDataResponses
        ) =>
            klineDataResponses.SelectMany(response =>
                response.Klines.Select(kline => new KlineDataCreationRequest
                {
                    IdTradingPair = response.IdTradingPair,
                    Kline = kline,
                })
            );

        /// <summary>
        /// Maps SvcKline kline data responses to ApiContracts kline data responses.
        /// </summary>
        public static IEnumerable<KlineDataResponse> ToApiContractKlineDataResponse(
            IEnumerable<SvcKline.KlineDataResponse> svcKlineResponses
        ) =>
            svcKlineResponses.Select(response => new KlineDataResponse
            {
                IdTradingPair = response.IdTradingPair,
                Klines = response.Klines,
            });
    }
}
