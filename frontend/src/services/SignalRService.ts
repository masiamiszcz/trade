import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import { API_CONFIG } from '../config/apiConfig';

export interface PriceUpdate {
  symbol: string;
  price: number;
  volume: number;
  timestamp: string;
}

export interface CandleUpdate {
  symbol: string;
  openTime: string;
  closeTime: string;
  open: number;
  high: number;
  low: number;
  close: number;
  volume: number;
  interval?: string;
}

export interface MarketStreamUpdate {
  tick?: PriceUpdate;
  candle?: CandleUpdate;
}

export interface SignalRServiceOptions {
  onPriceUpdate: (update: PriceUpdate) => void;
  onCandleUpdate?: (update: CandleUpdate) => void;
  onConnected?: () => void;
  onDisconnected?: (error?: Error | string) => void;
  accessTokenFactory?: () => string | null | Promise<string | null>;
}

export class SignalRService {
  private connection: HubConnection | null = null;
  private options: SignalRServiceOptions;
  private hubUrl = `${API_CONFIG.baseUrl.replace(/\/api\/?$/, '')}/hubs/prices`;
  private currentSymbol?: string;
  private currentRangeMinutes?: number;

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

    this.connection.on('ReceiveMarketUpdate', this.handleMarketUpdate);
    this.connection.onreconnected(async () => {
      this.options.onConnected?.();
      console.log('[SignalR] Reconnected to hub');

      if (this.currentSymbol && this.currentRangeMinutes !== undefined) {
        try {
          await this.subscribeToSymbol(this.currentSymbol, this.currentRangeMinutes);
        } catch (error) {
          console.warn('[SignalR] Failed to re-subscribe after reconnect', error);
        }
      }
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
    } finally {
      this.currentSymbol = undefined;
      this.currentRangeMinutes = undefined;
      this.connection = null;
    }
  }

  public isConnected(): boolean {
    return this.connection?.state === HubConnectionState.Connected;
  }

  public async subscribeToSymbol(symbol: string, rangeMinutes: number): Promise<void> {
    if (!this.connection || this.connection.state !== HubConnectionState.Connected) {
      throw new Error('SignalR connection is not established');
    }

    const normalizedSymbol = symbol.toUpperCase();
    if (this.currentSymbol === normalizedSymbol && this.currentRangeMinutes === rangeMinutes) {
      console.log('[SignalR] Subscription already active for', normalizedSymbol, 'rangeMinutes', rangeMinutes);
      return;
    }

    this.currentSymbol = normalizedSymbol;
    this.currentRangeMinutes = rangeMinutes;

    await this.connection.invoke('SubscribeToSymbol', this.currentSymbol, this.currentRangeMinutes);
    console.log('[SignalR] Subscribed to symbol', this.currentSymbol, 'rangeMinutes', this.currentRangeMinutes);
  }

  public async unsubscribeFromSymbol(symbol: string): Promise<void> {
    if (!this.connection || this.connection.state !== HubConnectionState.Connected) {
      return;
    }

    await this.connection.invoke('UnsubscribeFromSymbol', symbol.toUpperCase());
    console.log('[SignalR] Unsubscribed from symbol', symbol.toUpperCase());

    if (this.currentSymbol === symbol.toUpperCase()) {
      this.currentSymbol = undefined;
      this.currentRangeMinutes = undefined;
    }
  }

  private handleMarketUpdate = (update: MarketStreamUpdate): void => {
    if (update.tick) {
      this.options.onPriceUpdate(update.tick);
    }

    if (update.candle) {
      this.options.onCandleUpdate?.(update.candle);
    }
  };
}
