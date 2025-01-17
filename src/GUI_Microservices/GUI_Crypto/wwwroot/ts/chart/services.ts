import { KlineDataRequest } from './interfaces/kline-data-request';
import { KlineData } from './interfaces/kline-data';

export async function fetchKlineData(
    request: KlineDataRequest
): Promise<KlineData[]> {
    const params = new URLSearchParams({
        CoinMainSymbol: request.coinMainSymbol,
        CoinQuoteSymbol: request.coinQuoteSymbol,
        Interval: request.interval,
        StartTime: request.startTime,
        EndTime: request.endTime
    });

    const response = await fetch(`/chart/klines?${params}`);
    return response.json();
}
