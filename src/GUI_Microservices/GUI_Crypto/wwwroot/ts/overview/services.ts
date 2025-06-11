import { OverviewCoin } from './interfaces/overview-coin';
import { CandidateCoin } from './interfaces/candidate-coin';
import { CoinCreationRequest } from './interfaces/coin-creation-request';

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
