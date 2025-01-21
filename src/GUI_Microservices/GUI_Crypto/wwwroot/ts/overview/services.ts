import { ListedCoins } from './interfaces/listed-coins';
import { CoinNew } from './interfaces/coin-new';

export async function fetchListedCoins(): Promise<ListedCoins> {
    const response = await fetch('/listed-coins');
    if (!response.ok) throw { status: response.status };
    return response.json();
}

export async function createCoin(coin: CoinNew): Promise<boolean> {
    const response = await fetch('/coin', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(coin)
    });
    
    if (!response.ok) throw { status: response.status };
    return true;
}

export async function deleteCoin(id: number): Promise<void> {
    const response = await fetch(`/coin/${id}`, {
        method: 'DELETE'
    });
    
    if (!response.ok) throw { status: response.status };
} 
