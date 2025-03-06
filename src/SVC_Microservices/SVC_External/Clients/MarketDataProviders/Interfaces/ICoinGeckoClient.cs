using System.Collections.Frozen;
using SVC_External.Models.MarketDataProviders.Output;

namespace SVC_External.Clients.MarketDataProviders.Interfaces;

/// <summary>
/// Interface for interacting with CoinGecko API.
/// </summary>
public interface ICoinGeckoClient
{
    /// <summary>
    /// Gets all available coins list from CoinGecko.
    /// </summary>
    /// <returns>Collection of coins from CoinGecko.
    /// If the request fails, an empty collection is returned.</returns>
    Task<IEnumerable<CoinCoinGecko>> GetCoinsList();

    /// <summary>
    /// Retrieves all available on CoinGecko coins for a specific exchange and creates
    /// a map of symbols to their corresponding CoinGecko IDs out of them.
    /// </summary>
    /// <param name="idExchange">The CoinGecko ID of the exchange.</param>
    /// <returns>Dictionary mapping symbols to their corresponding CoinGecko IDs.
    /// Null value means that no CoinGecko data for the given symbol on the given exchange was found.
    /// If the request fails, an empty dictionary is returned.</returns>
    Task<FrozenDictionary<string, string?>> GetSymbolToIdMapForExchange(string idExchange);

    /// <summary>
    /// Gets market data for specified coin IDs.
    /// </summary>
    /// <param name="ids">Collection of CoinGecko coin IDs</param>
    /// <returns>Collection of assets from CoinGecko.
    /// If the request fails, an empty collection is returned.</returns>
    Task<IEnumerable<AssetCoinGecko>> GetCoinsMarkets(IEnumerable<string> ids);

    /// <summary>
    /// Gets all stablecoin IDs from CoinGecko.
    /// </summary>
    /// <returns>Collection of stablecoin IDs.
    /// If the request fails, an empty collection is returned.</returns>
    Task<IEnumerable<string>> GetStablecoinsIds();
}
