import { Kline } from '../../overview/interfaces/kline';

/**
 * Kline Data SignalR Interfaces
 */

/**
 * Represents kline data for a trading pair (matching ServiceModels.KlineDataUpdateMessage)
 */
export interface KlineDataUpdate {
    idTradingPair: number;
    klines: Kline[];
}

/**
 * Kline data update event arguments - now handles collections
 */
export interface KlineDataUpdateEventArgs {
    klineDataUpdates: KlineDataUpdate[];
    timestamp: Date;
}
