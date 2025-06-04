import { Exchange } from "./shared/exchange";

export interface CoinCreationRequest {
    id?: number | null;
    symbol: string;
    name: string;
    category?: CoinCategory | null;
    idCoinGecko?: string | null;
    tradingPairs: CoinCreationTradingPair[];
}

export interface CoinCreationTradingPair {
    coinQuote: CoinCreationCoinQuote;
    exchanges: Exchange[];
}

export interface CoinCreationCoinQuote {
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
