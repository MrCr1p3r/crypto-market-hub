import { Exchange } from "./shared/exchange";

export interface CandidateCoin {
    id?: number | null;
    symbol: string;
    name: string;
    category?: CoinCategory | null;
    idCoinGecko?: string | null;
    tradingPairs: CandidateCoinTradingPair[];
}

export interface CandidateCoinTradingPair {
    coinQuote: CandidateCoinTradingPairCoinQuote;
    exchanges: Exchange[];
}

export interface CandidateCoinTradingPairCoinQuote {
    id?: number | null;
    symbol: string;
    name: string;
    category?: CoinCategory | null;
    idCoinGecko?: string | null;
}

export enum CoinCategory {
    Stablecoin = 'Stablecoin',
    Fiat = 'Fiat',
}
