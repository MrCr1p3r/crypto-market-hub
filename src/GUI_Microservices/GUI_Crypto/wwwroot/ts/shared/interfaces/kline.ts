/**
 * Shared Kline interface for cryptocurrency market data
 * Represents candlestick/kline data across all application modules
 */
export interface Kline {
    openTime: number;
    openPrice: string;
    highPrice: string;
    lowPrice: string;
    closePrice: string;
    volume: string;
    closeTime: number;
}
