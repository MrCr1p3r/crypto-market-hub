import { KlineData } from './kline-data';
import { Exchange } from './shared/exchange';

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
        exchanges: Exchange[];
    } | null;
}
