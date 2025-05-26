using FluentResults;
using SVC_External.ApiContracts.Requests;
using SVC_External.ApiContracts.Responses.Exchanges.KlineData;

namespace SVC_External.Services.Exchanges.Interfaces;

/// <summary>
/// Service interface for managing kline data from exchanges.
/// </summary>
public interface IKlineDataService
{
    /// <summary>
    /// Retrieves Kline data for a trading pair from the first exchange that successfully returns data. <br/>
    /// The method attempts to fetch data from multiple exchanges sequentially
    /// and stops at the first successful result.
    /// </summary>
    /// <param name="request">
    /// Contains the trading pair data together with the exchanges from which kline data
    /// will be retrieved and the parameters for kline data retrieval.
    /// </param>
    /// <returns>
    /// Success: Result containing Kline data from the first responding exchange. <br/>
    /// Failure: Result with an error object describing the failure inside.
    /// </returns>
    Task<Result<KlineDataResponse>> GetKlineDataForTradingPair(KlineDataRequest request);

    /// <summary>
    /// For each coin in the request, retrieves Kline data for only its first successful trading pair. <br/>
    /// For each trading pair, the method attempts exchanges sequentially and stops at the first successful result. <br/>
    /// No data is returned for remaining trading pairs of a coin once a successful pair is found.
    /// </summary>
    /// <param name="request">
    /// Contains multiple coins, each with multiple trading pairs and eligible exchanges.
    /// Also includes parameters for kline data retrieval.
    /// </param>
    /// <returns>
    /// A collection of KlineDataResponse objects, where each response contains the trading pair ID and kline data.
    /// Each coin will have at most one entry in the collection (for its first successful trading pair).
    /// </returns>
    Task<IEnumerable<KlineDataResponse>> GetFirstSuccessfulKlineDataPerCoin(
        KlineDataBatchRequest request
    );
}
