export enum ExchangeKlineInterval {
    OneMinute = 1,
    FiveMinutes = 5,
    FifteenMinutes = 15,
    ThirtyMinutes = 30,
    OneHour = 60,
    FourHours = 240,
    OneDay = 1440,
    OneWeek = 10080,
    OneMonth = 43200,
}

export interface KlineDataRequestCoin {
    id: number;
    symbol: string;
    name: string;
}

export enum Exchange {
    Binance = 1,
    Bybit = 2,
    Mexc = 3,
}

export interface KlineDataRequest {
    idTradingPair: number;
    coinMain: KlineDataRequestCoin;
    coinQuote: KlineDataRequestCoin;
    exchanges: Exchange[];
    interval: ExchangeKlineInterval;
    startTime: string;
    endTime: string;
    limit: number;
}
