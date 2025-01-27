import { KlineData } from './kline-data';

export interface OverviewCoin {
    id: number;
    symbol: string;
    name: string;
    isStablecoin: boolean;
    klineData: KlineData[];
    tradingPair: {
        id: number;
        coinQuote: {
            id: number;
            symbol: string;
            name: string;
        };
    } | null;
} 
