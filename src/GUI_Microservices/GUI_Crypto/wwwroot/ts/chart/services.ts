import { KlineDataRequest } from './interfaces/kline-data-request';
import { KlineData } from './interfaces/kline-data';

export async function fetchKlineData(request: KlineDataRequest): Promise<KlineData[]> {
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
