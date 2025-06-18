import * as signalR from '@microsoft/signalr';
import { ConnectionStatus, SignalRConfiguration, ConnectionEventArgs } from './interfaces/signalr';
import { CoinMarketData, MarketDataUpdateEventArgs } from './interfaces/market-data-signalr';
import { KlineDataUpdate, KlineDataUpdateEventArgs } from './interfaces/kline-signalr';

/**
 * Unified SignalR Crypto Connection Manager
 * Handles both market data and kline data updates via a single SignalR connection
 */
export class CryptoSignalR {
    private connection: signalR.HubConnection | null = null;
    private isConnected: boolean = false;
    private connectionRetryCount: number = 0;
    private readonly config: SignalRConfiguration;
    private eventListeners: Map<string, Function[]> = new Map();

    constructor(config?: Partial<SignalRConfiguration>) {
        this.config = {
            hubUrl: '/hubs/crypto',
            maxRetries: 5,
            retryDelayMs: 3000,
            reconnectIntervals: [0, 2000, 10000, 30000],
            logLevel: signalR.LogLevel.Information,
            ...config,
        };

        this.initializeConnection();
    }

    /**
     * Initialize SignalR connection with type safety
     */
    private initializeConnection(): void {
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl(this.config.hubUrl)
            .withAutomaticReconnect(this.config.reconnectIntervals)
            .configureLogging(this.config.logLevel)
            .build();

        this.setupEventHandlers();
        this.startConnection();
    }

    /**
     * Setup typed SignalR event handlers
     */
    private setupEventHandlers(): void {
        if (!this.connection) return;

        // Connection lifecycle events
        this.connection.onclose(async (error?: Error) => {
            this.isConnected = false;
            console.warn('Crypto SignalR connection closed:', error);
            this.updateConnectionStatus('disconnected', 'Connection lost');
            await this.attemptReconnection();
        });

        this.connection.onreconnecting((error?: Error) => {
            this.isConnected = false;
            console.warn('Crypto SignalR reconnecting:', error);
            this.updateConnectionStatus('reconnecting', 'Attempting to reconnect...');
        });

        this.connection.onreconnected((connectionId?: string) => {
            this.isConnected = true;
            this.connectionRetryCount = 0;
            console.log('Crypto SignalR reconnected with ID:', connectionId);
            this.updateConnectionStatus('connected', 'Connected successfully', connectionId);

            // Re-subscribe to previously subscribed data types
            this.resubscribeAfterReconnection();
        });

        // Market data event handlers
        this.connection.on('ReceiveMarketDataUpdate', (marketData: CoinMarketData[]) => {
            this.handleMarketDataUpdate(marketData);
        });

        // Kline data event handlers
        this.connection.on('ReceiveKlineDataUpdate', (klineDataUpdates: KlineDataUpdate[]) => {
            this.handleKlineDataUpdate(klineDataUpdates);
        });

        // Cache warmup completion event handlers
        this.connection.on('ReceiveCacheWarmupCompleted', () => {
            this.emit('cacheWarmupCompleted', {});
        });
    }

    /**
     * Start the SignalR connection
     */
    public async startConnection(): Promise<void> {
        if (!this.connection) {
            throw new Error('Connection not initialized');
        }

        try {
            await this.connection.start();
            this.isConnected = true;
            this.connectionRetryCount = 0;
            this.updateConnectionStatus('connected', 'Connected successfully');
        } catch (error) {
            this.isConnected = false;
            console.error('Crypto SignalR connection failed:', error);
            this.updateConnectionStatus('failed', 'Connection failed');
            await this.attemptReconnection();
        }
    }

    /**
     * Subscribe to overview updates
     */
    public async subscribeToOverviewUpdates(): Promise<void> {
        if (this.isConnected && this.connection) {
            await this.connection.invoke('SubscribeToOverviewUpdates');
        } else {
            console.error('Crypto SignalR connection not established');
        }
    }

    /**
     * Unsubscribe from overview updates
     */
    public async unsubscribeFromOverviewUpdates(): Promise<void> {
        if (this.isConnected && this.connection) {
            await this.connection.invoke('UnsubscribeFromOverviewUpdates');
        } else {
            console.error('Crypto SignalR connection not established');
        }
    }

    /**
     * Re-subscribe to data after reconnection
     */
    private async resubscribeAfterReconnection(): Promise<void> {
        // For simplicity, subscribe to all crypto data on reconnection
        // In a more sophisticated implementation, you'd track what was previously subscribed
        await this.subscribeToOverviewUpdates();
    }

    /**
     * Attempt reconnection with exponential backoff
     */
    private async attemptReconnection(): Promise<void> {
        if (this.connectionRetryCount >= this.config.maxRetries) {
            console.error('Max reconnection attempts reached');
            this.updateConnectionStatus('failed', 'Max reconnection attempts reached');
            return;
        }

        this.connectionRetryCount++;
        const delay = this.config.retryDelayMs * Math.pow(2, this.connectionRetryCount - 1);

        console.log(
            `Attempting reconnection ${this.connectionRetryCount}/${this.config.maxRetries} in ${delay}ms...`
        );

        setTimeout(async () => {
            await this.startConnection();
        }, delay);
    }

    /**
     * Handle market data updates
     */
    private handleMarketDataUpdate(marketData: CoinMarketData[]): void {
        const eventArgs: MarketDataUpdateEventArgs = {
            marketData,
            timestamp: new Date(),
            source: 'bulk',
        };

        this.emit('marketDataUpdate', eventArgs);
    }

    /**
     * Handle kline data updates
     */
    private handleKlineDataUpdate(klineDataUpdates: KlineDataUpdate[]): void {
        const eventArgs: KlineDataUpdateEventArgs = {
            klineDataUpdates,
            timestamp: new Date(),
        };

        this.emit('klineDataUpdate', eventArgs);
    }

    /**
     * Update connection status with typed parameters
     */
    private updateConnectionStatus(
        status: ConnectionStatus,
        message: string,
        connectionId?: string
    ): void {
        const eventArgs: ConnectionEventArgs = {
            status,
            message,
            timestamp: new Date(),
            connectionId,
        };

        this.emit('connectionStatusChanged', eventArgs);
        this.updateConnectionStatusUI(status, message);
    }

    /**
     * Update connection status in UI elements
     */
    private updateConnectionStatusUI(status: ConnectionStatus, message: string): void {
        const statusElement = document.querySelector('#signalr-status') as HTMLElement;
        if (statusElement) {
            statusElement.textContent = message;
            statusElement.className = `signalr-status status-${status}`;
        }

        const indicator = document.querySelector('#connection-indicator') as HTMLElement;
        if (indicator) {
            indicator.className = `connection-indicator ${status}`;
            indicator.title = message;
        }
    }

    /**
     * Event listener management
     */
    public on(event: string, listener: Function): void {
        if (!this.eventListeners.has(event)) {
            this.eventListeners.set(event, []);
        }
        this.eventListeners.get(event)!.push(listener);
    }

    public off(event: string, listener: Function): void {
        const listeners = this.eventListeners.get(event);
        if (listeners) {
            const index = listeners.indexOf(listener);
            if (index > -1) {
                listeners.splice(index, 1);
            }
        }
    }

    private emit(event: string, ...args: unknown[]): void {
        const listeners = this.eventListeners.get(event);
        if (listeners) {
            listeners.forEach((listener) => listener(...args));
        }
    }

    /**
     * Check if currently connected
     */
    public get connected(): boolean {
        return this.isConnected;
    }

    /**
     * Stop the connection and cleanup
     */
    public async stop(): Promise<void> {
        if (this.connection) {
            await this.connection.stop();
            this.isConnected = false;
            console.log('Crypto SignalR connection stopped');
        }
    }

    /**
     * Dispose resources
     */
    public dispose(): void {
        this.eventListeners.clear();
        this.stop();
    }
}
