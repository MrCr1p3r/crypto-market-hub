import * as signalR from '@microsoft/signalr';
import { cryptoManager } from '../realtime/crypto-manager';
import { TableManager } from './table/table-manager';
import { CoinMarketData } from './interfaces/coin-market-data';

/**
 * Initialize real-time market data connection for the overview page
 */
export async function initializeOverviewRealtime(tableManager: TableManager): Promise<void> {
    // Initialize the crypto connection with configuration
    await cryptoManager.init({
        hubUrl: '/hubs/crypto',
        maxRetries: 5,
        retryDelayMs: 3000,
        reconnectIntervals: [0, 2000, 10000, 30000],
        logLevel: signalR.LogLevel.Information,
    });

    // Subscribe to all crypto data (market data + kline data)
    await cryptoManager.subscribeToOverviewUpdates();

    // Add event listeners for custom events
    document.addEventListener('marketDataUpdated', (event: Event) => {
        const customEvent = event as CustomEvent;
        const data = customEvent.detail;

        // Use the passed table manager instead of singleton
        if (!tableManager || !data) {
            console.warn('TableManager not available or no data received');
            return;
        }

        const updates = data.map((coin: CoinMarketData) => ({
            id: coin.id,
            priceUsd: coin.priceUsd,
            marketCapUsd: coin.marketCapUsd,
            priceChangePercentage24h: coin.priceChangePercentage24h,
        }));

        tableManager.updateCoinsMarketData(updates);
    });

    document.addEventListener('connectionStatusChanged', (event: Event) => {
        const customEvent = event as CustomEvent;
        const status = customEvent.detail;

        // Update connection status indicator in the UI
        const statusElement = document.querySelector('.connection-status');
        if (statusElement) {
            statusElement.textContent = status.status;
            statusElement.className = `connection-status ${status.status.toLowerCase()}`;

            if (status.status === 'Connected') {
                statusElement.classList.add('connected');
            } else {
                statusElement.classList.remove('connected');
            }
        }
    });

    // Add event listener for kline data updates
    document.addEventListener('klineDataUpdated', (event: Event) => {
        console.log('Kline data updates event received:', event);
        const customEvent = event as CustomEvent;
        const klineDataUpdates = customEvent.detail;

        if (!tableManager || !klineDataUpdates || !Array.isArray(klineDataUpdates)) {
            console.warn('TableManager not available or no kline data updates received');
            return;
        }

        // Update kline data in the table and refresh charts
        tableManager.updateKlineData(klineDataUpdates);
    });
}
