import * as signalR from '@microsoft/signalr';

/**
 * Connection status types
 */
export type ConnectionStatus = 'connected' | 'reconnecting' | 'disconnected' | 'failed';

/**
 * Configuration options for SignalR connection
 */
export interface SignalRConfiguration {
    hubUrl: string;
    maxRetries: number;
    retryDelayMs: number;
    reconnectIntervals: number[];
    logLevel: signalR.LogLevel;
}

/**
 * Connection event arguments
 */
export interface ConnectionEventArgs {
    status: ConnectionStatus;
    message: string;
    timestamp: Date;
    connectionId?: string | undefined;
}
