import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import { API_CONFIG } from '../config/apiConfig';

export interface PriceUpdate {
  symbol: string;
  price: number;
  volume: number;
  timestamp: string;
}

export interface SignalRServiceOptions {
  onPriceUpdate: (update: PriceUpdate) => void;
  onConnected?: () => void;
  onDisconnected?: (error?: Error | string) => void;
  accessTokenFactory?: () => string | null | Promise<string | null>;
}

export class SignalRService {
  private connection: HubConnection | null = null;
  private options: SignalRServiceOptions;
  private hubUrl = `${API_CONFIG.baseUrl.replace(/\/api\/?$/, '')}/hubs/prices`;

  constructor(options: SignalRServiceOptions) {
    this.options = options;
  }

  public async connect(): Promise<void> {
    if (this.connection && this.connection.state !== HubConnectionState.Disconnected) {
      return;
    }

    this.connection = new HubConnectionBuilder()
      .withUrl(this.hubUrl, {
        accessTokenFactory: this.options.accessTokenFactory,
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    this.connection.on('ReceivePriceUpdate', this.handlePriceUpdate);
    this.connection.onreconnected(() => {
      this.options.onConnected?.();
      console.log('[SignalR] Reconnected to hub');
    });
    this.connection.onclose((error) => {
      this.options.onDisconnected?.(error ?? 'SignalR connection closed');
      console.warn('[SignalR] Connection closed', error);
    });

    try {
      await this.connection.start();
      this.options.onConnected?.();
      console.log('[SignalR] Connected to', this.hubUrl);
    } catch (error) {
      console.error('[SignalR] Failed to connect', error);
      this.options.onDisconnected?.(error instanceof Error ? error : String(error));
    }
  }

  public async disconnect(): Promise<void> {
    if (!this.connection) {
      return;
    }

    try {
      await this.connection.stop();
      console.log('[SignalR] Disconnected');
    } catch (error) {
      console.warn('[SignalR] Error during disconnect', error);
    }
  }

  public isConnected(): boolean {
    return this.connection?.state === HubConnectionState.Connected;
  }

  public async subscribeToSymbol(symbol: string): Promise<void> {
    if (!this.connection || this.connection.state !== HubConnectionState.Connected) {
      throw new Error('SignalR connection is not established');
    }

    await this.connection.invoke('SubscribeToSymbol', symbol.toUpperCase());
    console.log('[SignalR] Subscribed to symbol', symbol.toUpperCase());
  }

  public async unsubscribeFromSymbol(symbol: string): Promise<void> {
    if (!this.connection || this.connection.state !== HubConnectionState.Connected) {
      return;
    }

    await this.connection.invoke('UnsubscribeFromSymbol', symbol.toUpperCase());
    console.log('[SignalR] Unsubscribed from symbol', symbol.toUpperCase());
  }

  private handlePriceUpdate = (update: PriceUpdate): void => {
    this.options.onPriceUpdate(update);
  };
}
