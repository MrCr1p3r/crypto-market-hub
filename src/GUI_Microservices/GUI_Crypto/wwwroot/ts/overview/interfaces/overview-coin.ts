import { Kline } from '../../shared/interfaces/kline';

export interface OverviewCoin {
    id: number;
    symbol: string;
    name: string;
    category: number | null;
    marketCapUsd: number | null;
    priceUsd: string | null;
    priceChangePercentage24h: number | null;
    tradingPairIds: number[];
    klineData: {
        tradingPairId: number;
        klines: Kline[];
    } | null;
}
