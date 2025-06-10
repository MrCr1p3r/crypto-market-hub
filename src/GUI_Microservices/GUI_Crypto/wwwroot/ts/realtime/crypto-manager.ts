import { CryptoSignalR } from './crypto-signalr';
import { SignalRConfiguration, ConnectionEventArgs } from './interfaces/signalr';
import { KlineDataUpdateEventArgs } from './interfaces/kline-signalr';
import { MarketDataUpdateEventArgs } from './interfaces/market-data-signalr';

/**
 * Unified Crypto Data Manager
 * Manages both market data and kline data via a single SignalR connection
 */
export class CryptoManager {
    private signalRClient: CryptoSignalR | null = null;

    /**
     * Initialize the crypto connection
     */
    public async init(config?: Partial<SignalRConfiguration>): Promise<void> {
        this.signalRClient = new CryptoSignalR(config);
        this.setupEventListeners();

        // Wait for connection to be established
        await this.waitForConnection();
    }

    /**
     * Wait for the SignalR connection to be established
     */
    public async waitForConnection(): Promise<void> {
        return new Promise<void>((resolve, reject) => {
            if (!this.signalRClient) {
                reject(new Error('SignalR client not initialized'));
                return;
            }

            // If already connected, resolve immediately
            if (this.signalRClient.connected) {
                resolve();
                return;
            }

            // Set up a one-time listener for connection establishment
            const connectionHandler = (args: ConnectionEventArgs): void => {
                if (args.status === 'connected') {
                    this.signalRClient?.off('connectionStatusChanged', connectionHandler);
                    resolve();
                } else if (args.status === 'failed') {
                    this.signalRClient?.off('connectionStatusChanged', connectionHandler);
                    reject(new Error(`Connection failed: ${args.message}`));
                }
            };

            this.signalRClient.on('connectionStatusChanged', connectionHandler);

            // Timeout after 10 seconds
            setTimeout(() => {
                this.signalRClient?.off('connectionStatusChanged', connectionHandler);
                reject(new Error('Connection timeout'));
            }, 10000);
        });
    }

    /**
     * Setup event listeners for SignalR events
     */
    private setupEventListeners(): void {
        if (!this.signalRClient) return;

        // Market data update events
        this.signalRClient.on('marketDataUpdate', (args: MarketDataUpdateEventArgs) => {
            console.log('Market data updated:', args);
            this.onMarketDataUpdate(args);
        });

        // Kline data update events
        this.signalRClient.on('klineDataUpdate', (args: KlineDataUpdateEventArgs) => {
            console.log('Kline data updated:', args);
            this.onKlineDataUpdate(args);
        });

        // Connection status events
        this.signalRClient.on('connectionStatusChanged', (args: ConnectionEventArgs) => {
            this.onConnectionStatusChanged(args);
        });
    }

    /**
     * Handle market data updates
     */
    private onMarketDataUpdate(args: MarketDataUpdateEventArgs): void {
        this.dispatchCustomEvent('marketDataUpdated', args.marketData);
    }

    /**
     * Handle kline data updates
     */
    private onKlineDataUpdate(args: KlineDataUpdateEventArgs): void {
        this.dispatchCustomEvent('klineDataUpdated', args.klineDataUpdates);
    }

    /**
     * Handle connection status changes
     */
    private onConnectionStatusChanged(args: ConnectionEventArgs): void {
        this.updateGlobalConnectionStatus(args.status, args.message);
        this.dispatchCustomEvent('connectionStatusChanged', args);
    }

    /**
     * Update global connection status indicators
     */
    private updateGlobalConnectionStatus(
        status: import('./interfaces/signalr.js').ConnectionStatus,
        message: string
    ): void {
        // Update navbar indicator if exists
        const navbarIndicator = document.querySelector('#navbar-connection-status') as HTMLElement;
        if (navbarIndicator) {
            navbarIndicator.className = `connection-status ${status}`;
            navbarIndicator.title = message;
        }

        // Update footer status if exists
        const footerStatus = document.querySelector('#footer-realtime-status') as HTMLElement;
        if (footerStatus) {
            footerStatus.textContent = `Real-time: ${message}`;
            footerStatus.className = `realtime-status ${status}`;
        }
    }

    /**
     * Dispatch custom DOM events
     */
    private dispatchCustomEvent(eventName: string, detail: unknown): void {
        const event = new CustomEvent(eventName, {
            detail,
            bubbles: true,
            cancelable: true,
        });

        document.dispatchEvent(event);
    }

    /**
     * Subscribe to overview updates
     */
    public async subscribeToOverviewUpdates(): Promise<void> {
        if (!this.signalRClient) {
            console.warn('SignalR client not initialized');
            return;
        }

        // Wait for connection if not connected
        if (!this.signalRClient.connected) {
            console.log('Waiting for connection before subscribing...');
            await this.waitForConnection();
        }

        await this.signalRClient.subscribeToOverviewUpdates();
    }

    /**
     * Unsubscribe from overview updates
     */
    public async unsubscribeFromOverviewUpdates(): Promise<void> {
        if (this.signalRClient) {
            await this.signalRClient.unsubscribeFromOverviewUpdates();
        } else {
            console.warn('SignalR client not initialized');
        }
    }

    /**
     * Check if currently connected
     */
    public get isConnected(): boolean {
        return this.signalRClient?.connected || false;
    }

    /**
     * Get the SignalR client instance
     */
    public getClient(): CryptoSignalR | null {
        return this.signalRClient;
    }

    /**
     * Restart the connection
     */
    public async restart(): Promise<void> {
        if (this.signalRClient) {
            await this.signalRClient.stop();
            await this.signalRClient.startConnection();
        }
    }

    /**
     * Dispose resources and cleanup
     */
    public dispose(): void {
        if (this.signalRClient) {
            this.signalRClient.dispose();
            this.signalRClient = null;
        }
    }
}

// Create and export a single instance for the application
export const cryptoManager = new CryptoManager();
