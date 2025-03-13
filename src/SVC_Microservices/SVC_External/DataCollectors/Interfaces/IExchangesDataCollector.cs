using FluentResults;
using SVC_External.Models.Input;
using SVC_External.Models.Output;

namespace SVC_External.DataCollectors.Interfaces;

/// <summary>
/// Generic interface for exchange clients that fetch Kline data.
/// </summary>
public interface IExchangesDataCollector
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
    Task<Result<KlineDataRequestResponse>> GetKlineDataForTradingPair(KlineDataRequest request);

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
    /// A collection of Kline data, one per coin (if available),
    /// </returns>
    Task<IEnumerable<KlineDataRequestResponse>> GetFirstSuccessfulKlineDataPerCoin(
        KlineDataBatchRequest request
    );

    /// <summary>
    /// Fetches all spot coins from the available exchanges.
    /// </summary>
    /// <returns>
    /// Success: Result containing a collection of coins, listen on all of the available exchanges. <br/>
    /// Failure: Result with an error object describing the failure inside.
    /// </returns>
    Task<Result<IEnumerable<Coin>>> GetAllCurrentActiveSpotCoins();

    /// <summary>
    /// Fetches coingecko asset information for the provided ids.
    /// </summary>
    /// <param name="ids">Collection of CoinGecko IDs.</param>
    /// <returns>
    /// Success: Result containing a collection of coingecko asset information objects. <br/>
    /// Failure: Result with an error object describing the failure inside.
    /// </returns>
    Task<Result<IEnumerable<CoinGeckoAssetInfo>>> GetCoinGeckoAssetsInfo(IEnumerable<string> ids);
}
