import { Exchange } from "./shared/exchange";


export interface KlineDataRequestCoin {
    id: number;
    symbol: string;
    name: string;
}

export interface KlineDataRequest {
    idTradingPair: number;
    coinMain: KlineDataRequestCoin;
    coinQuote: KlineDataRequestCoin;
    exchanges: Exchange[];
}
