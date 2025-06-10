/**
 * Market Data SignalR Interfaces
 */

/**
 * Represents market data for a single coin
 */
export interface CoinMarketData {
    id: number;
    priceUsd?: string;
    marketCapUsd?: number;
    priceChangePercentage24h?: number;
}

/**
 * Market data update event arguments
 */
export interface MarketDataUpdateEventArgs {
    marketData: CoinMarketData[];
    timestamp: Date;
    source: 'bulk';
}
