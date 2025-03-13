using FluentResults;
using SharedLibrary.Enums;
using SVC_External.Models.Exchanges.Input;
using SVC_External.Models.Exchanges.Output;

namespace SVC_External.Clients.Exchanges.Interfaces;

/// <summary>
/// Generic interface for exchange clients that fetch Kline data.
/// </summary>
public interface IExchangesClient
{
    /// <summary>
    /// Gets the name of the exchange.
    /// </summary>
    Exchange CurrentExchange { get; }

    /// <summary>
    /// Fetches all listed coins from the exchange.
    /// </summary>
    /// <returns>
    /// Success: Result containing a collection of all listed coins from the exchange. <br/>
    /// Failure: Result with an error object describing the failure inside.
    /// </returns>
    Task<Result<IEnumerable<ExchangeCoin>>> GetAllSpotCoins();

    /// <summary>
    /// Fetches Kline (candlestick) data from the exchange.
    /// </summary>
    /// <param name="request">The request parameters for fetching Kline data.</param>
    /// <returns>
    /// Success: Result containing a collection of retrieved Kline data objects. <br/>
    /// Failure: Result with an error object describing the failure inside.
    /// </returns>
    Task<Result<IEnumerable<ExchangeKlineData>>> GetKlineData(ExchangeKlineDataRequest request);
}
