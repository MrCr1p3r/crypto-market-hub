import { OverviewCoin } from './interfaces/overview-coin';
import { CandidateCoin } from './interfaces/candidate-coin';
import { CoinCreationRequest } from './interfaces/coin-creation-request';
import { KlineDataRequest } from './interfaces/kline-data-request';

// @ts-ignore - Ignore type errors for browser APIs
declare const fetch: any;
// @ts-ignore - Ignore type errors for browser APIs
declare const window: any;

export async function fetchCoins(): Promise<OverviewCoin[]> {
    const response = await fetch('/coins');
    if (!response.ok) throw { status: response.status };
    return response.json();
}

export async function fetchCandidateCoins(): Promise<CandidateCoin[]> {
    const response = await fetch('/candidate-coins');
    if (!response.ok) throw { status: response.status };
    return response.json();
}

export async function createCoins(
    coins: CoinCreationRequest[]
): Promise<{ isSuccess: boolean; error?: string }> {
    const response = await fetch('/coins', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify(coins),
    });

    if (!response.ok) {
        const error = await response.text();
        return { isSuccess: false, error };
    }
    return { isSuccess: true };
}

export async function deleteCoin(id: number): Promise<void> {
    const response = await fetch(`/coins/${id}`, {
        method: 'DELETE',
    });

    if (!response.ok) throw { status: response.status };
}

export async function resetDatabase(): Promise<boolean> {
    const response = await fetch('/coins', {
        method: 'DELETE',
    });
    return response.ok;
}

export async function openChart(coin: OverviewCoin): Promise<void> {
    if (!coin.tradingPair) {
        throw new Error('No trading pair available for this coin');
    }

    // Create the request object
    const request: KlineDataRequest = {
        idTradingPair: coin.tradingPair.id,
        coinMain: {
            id: coin.id,
            symbol: coin.symbol,
            name: coin.name,
        },
        coinQuote: {
            id: coin.tradingPair.coinQuote.id,
            symbol: coin.tradingPair.coinQuote.symbol,
            name: coin.tradingPair.coinQuote.name,
        },
        exchanges: coin.tradingPair.exchanges,
    };

    // Send POST request to chart endpoint
    const response = await fetch('/chart', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify(request),
    });

    if (!response.ok) {
        throw new Error(`Failed to open chart: ${response.statusText}`);
    }

    // Open the chart response in a new window
    const chartHtml = await response.text();
    const newWindow = window.open('', '_blank');
    if (newWindow) {
        newWindow.document.write(chartHtml);
        newWindow.document.close();
    }
}
