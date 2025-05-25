using System.Collections.Frozen;
using FluentResults;
using SVC_External.ExternalClients.MarketDataProviders.CoinGecko.Contracts.Responses;

namespace SVC_External.ExternalClients.MarketDataProviders.CoinGecko;

/// <summary>
/// Interface for interacting with CoinGecko API.
/// </summary>
public interface ICoinGeckoClient
{
    /// <summary>
    /// Retrieves all active available coins from CoinGecko.
    /// </summary>
    /// <returns>
    /// Success: Result containing a collection of CoinGecko coins. <br/>
    /// Failure: Result with an error object describing the failure inside.
    /// </returns>
    Task<Result<IEnumerable<CoinCoinGecko>>> GetCoinsList();

    /// <summary>
    /// Retrieves all available on CoinGecko coins for a specific exchange and creates
    /// a map of symbols to their corresponding CoinGecko IDs out of them.
    /// </summary>
    /// <param name="idExchange">The CoinGecko ID of the exchange.</param>
    /// <returns>
    /// Success: Result containing a frozen dictionary mapping symbols to their CoinGecko IDs.
    /// A null value in the dictionary means no CoinGecko data was found for that symbol. <br/>
    /// Failure: Result with an error object describing the failure inside.
    /// </returns>
    Task<Result<FrozenDictionary<string, string?>>> GetSymbolToIdMapForExchange(string idExchange);

    /// <summary>
    /// Fetches market data for specified CoinGecko IDs.
    /// </summary>
    /// <param name="ids">Collection of CoinGecko coin IDs. </param>
    /// <returns>
    /// Success: Result containing a collection of CoinGecko assets with market data. <br/>
    /// Failure: Result with an error object describing the failure inside.
    /// </returns>
    Task<Result<IEnumerable<AssetCoinGecko>>> GetMarketDataForCoins(IEnumerable<string> ids);

    /// <summary>
    /// Retrieves all stablecoin IDs from CoinGecko.
    /// </summary>
    /// <returns>
    /// Success: Result containing a collection of stablecoin IDs. <br/>
    /// Failure: Result with an error object describing the failure inside.
    /// </returns>
    Task<Result<IEnumerable<string>>> GetStablecoinsIds();
}
