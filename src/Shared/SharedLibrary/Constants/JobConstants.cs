namespace SharedLibrary.Constants;

/// <summary>
/// Contains constants for scheduled job names and types.
/// </summary>
public static class JobConstants
{
    /// <summary>
    /// Job names for different scheduled jobs.
    /// </summary>
    public static class Names
    {
        /// <summary>
        /// Market data update job name.
        /// </summary>
        public const string MarketDataUpdate = "Market Data Update";

        /// <summary>
        /// Kline data update job name.
        /// </summary>
        public const string KlineDataUpdate = "Kline Data Update";

        /// <summary>
        /// Trading pairs update job name.
        /// </summary>
        public const string TradingPairsUpdate = "Trading Pairs Update";
    }

    /// <summary>
    /// Job types/categories for different kinds of jobs.
    /// </summary>
    public static class Types
    {
        /// <summary>
        /// Data synchronization job type.
        /// </summary>
        public const string DataSync = "DataSync";
    }

    /// <summary>
    /// Exchange names for message routing.
    /// </summary>
    public static class Exchanges
    {
        /// <summary>
        /// Main exchange for scheduler service messages.
        /// </summary>
        public const string CryptoScheduler = "crypto.scheduler";
    }

    /// <summary>
    /// Routing keys for message publishing.
    /// </summary>
    public static class RoutingKeys
    {
        /// <summary>
        /// Routing key for market data update messages.
        /// </summary>
        public const string MarketDataUpdated = "svc.scheduler.marketdata.updated";

        /// <summary>
        /// Routing key for kline data update messages.
        /// </summary>
        public const string KlineDataUpdated = "svc.scheduler.klinedata.updated";

        /// <summary>
        /// Routing key for trading pairs update messages.
        /// </summary>
        public const string TradingPairsUpdated = "svc.scheduler.tradingpairs.updated";
    }

    /// <summary>
    /// Queue names for message consumption.
    /// </summary>
    public static class QueueNames
    {
        /// <summary>
        /// Queue for market data update messages in GUI.
        /// </summary>
        public const string GuiMarketDataUpdated = "gui.crypto.marketdata.updated";

        /// <summary>
        /// Queue for kline data update messages in GUI.
        /// </summary>
        public const string GuiKlineDataUpdated = "gui.crypto.klinedata.updated";

        /// <summary>
        /// Queue for trading pairs update messages in GUI.
        /// </summary>
        public const string GuiTradingPairsUpdated = "gui.crypto.tradingpairs.updated";

        /// <summary>
        /// Gets all GUI queue names.
        /// </summary>
        /// <returns>Collection of all GUI queue names.</returns>
        public static IEnumerable<string> GetAllGuiQueues()
        {
            yield return GuiMarketDataUpdated;
            yield return GuiKlineDataUpdated;
            yield return GuiTradingPairsUpdated;
        }

        /// <summary>
        /// Gets the queue-to-routing-key mappings for all GUI queues.
        /// </summary>
        /// <returns>Dictionary mapping queue names to their routing keys.</returns>
        public static Dictionary<string, string> GetGuiQueueBindings()
        {
            return new Dictionary<string, string>
            {
                [GuiMarketDataUpdated] = RoutingKeys.MarketDataUpdated,
                [GuiKlineDataUpdated] = RoutingKeys.KlineDataUpdated,
                [GuiTradingPairsUpdated] = RoutingKeys.TradingPairsUpdated,
            };
        }
    }

    /// <summary>
    /// Source identifiers for different services that create jobs.
    /// </summary>
    public static class Sources
    {
        /// <summary>
        /// Source identifier for the scheduler service.
        /// </summary>
        public const string Scheduler = "SVC_Scheduler";
    }
}
