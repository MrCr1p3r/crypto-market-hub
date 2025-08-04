import { KlineDataRequest } from './interfaces/kline-data-request';
import { Kline } from '../shared/interfaces/kline';

export async function fetchKlineData(request: KlineDataRequest): Promise<Kline[]> {
    const response = await fetch(`/chart/klines/query`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify(request),
    });
    if (!response.ok) {
        throw new Error(`Failed to fetch kline data: ${response.statusText}`);
    }
    return response.json();
}
