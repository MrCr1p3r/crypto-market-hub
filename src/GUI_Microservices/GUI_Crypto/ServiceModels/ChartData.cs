using GUI_Crypto.MicroserviceClients.SvcCoins.Contracts.Responses;
using GUI_Crypto.MicroserviceClients.SvcExternal.Contracts.Responses.KlineData;

namespace GUI_Crypto.ServiceModels;

/// <summary>
/// Aggregated data contract for chart view model creation.
/// </summary>
public class ChartData
{
    /// <summary>
    /// The main coin for which the chart will be displayed.
    /// </summary>
    public required Coin Coin { get; init; }

    /// <summary>
    /// Kline data for the specific trading pair from external exchanges.
    /// </summary>
    public required KlineDataResponse KlineResponse { get; init; }
}
