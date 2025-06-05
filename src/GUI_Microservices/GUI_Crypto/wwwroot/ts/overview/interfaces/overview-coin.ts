import { Kline } from './kline';
import { Exchange } from './shared/exchange';

export interface OverviewCoin {
    id: number;
    symbol: string;
    name: string;
    category: number | null;
    marketCapUsd: number | null;
    priceUsd: string | null;
    priceChangePercentage24h: number | null;
    klineData: {
        tradingPair: {
            id: number;
            coinQuote: {
                id: number;
                symbol: string;
                name: string;
            };
            exchanges: Exchange[];
        };
        klines: Kline[];
    } | null;
}
