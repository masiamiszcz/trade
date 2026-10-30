import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import { API_CONFIG } from '../config/apiConfig';

export interface PriceUpdate {
  symbol: string;
  price: number;
  volume: number;
  timestamp: string;
  updateId?: string;
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
  private currentSymbols: string[] = [];
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

      if (this.currentSymbols.length > 0 && this.currentRangeMinutes !== undefined) {
        const symbols = [...this.currentSymbols];
        const rangeMinutes = this.currentRangeMinutes;
        try {
          this.currentSymbols = [];
          await this.subscribeToSymbols(symbols, rangeMinutes);
        } catch (error) {
          this.currentSymbols = symbols;
          this.currentRangeMinutes = rangeMinutes;
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
      this.currentSymbols = [];
      this.currentRangeMinutes = undefined;
      this.connection = null;
    }
  }

  public isConnected(): boolean {
    return this.connection?.state === HubConnectionState.Connected;
  }

  public async subscribeToSymbol(symbol: string, rangeMinutes: number): Promise<void> {
    await this.subscribeToSymbols([symbol], rangeMinutes);
  }

  public async subscribeToSymbols(symbols: string[], rangeMinutes: number): Promise<void> {
    if (!this.connection || this.connection.state !== HubConnectionState.Connected) {
      throw new Error('SignalR connection is not established');
    }

    const normalizedSymbols = [...new Set(symbols.map((symbol) => symbol.trim().toUpperCase()).filter(Boolean))];
    if (normalizedSymbols.length === 0 || normalizedSymbols.length > 4) {
      throw new Error('SignalR subscriptions require between one and four symbols');
    }

    if (normalizedSymbols.length === this.currentSymbols.length
      && normalizedSymbols.every((symbol, index) => symbol === this.currentSymbols[index])
      && this.currentRangeMinutes === rangeMinutes) {
      console.log('[SignalR] Subscriptions already active for', normalizedSymbols, 'rangeMinutes', rangeMinutes);
      return;
    }

    if (normalizedSymbols.length === 1) {
      await this.connection.invoke('SubscribeToSymbol', normalizedSymbols[0], rangeMinutes);
    } else {
      await this.connection.invoke('SubscribeToSymbols', normalizedSymbols, rangeMinutes);
    }
    this.currentSymbols = normalizedSymbols;
    this.currentRangeMinutes = rangeMinutes;
    console.log('[SignalR] Subscribed to symbols', normalizedSymbols, 'rangeMinutes', rangeMinutes);
  }

  public async unsubscribeFromSymbol(symbol: string): Promise<void> {
    await this.unsubscribeFromSymbols([symbol]);
  }

  public async unsubscribeFromSymbols(symbols: string[]): Promise<void> {
    if (!this.connection || this.connection.state !== HubConnectionState.Connected) {
      return;
    }

    const normalizedSymbols = [...new Set(symbols.map((symbol) => symbol.trim().toUpperCase()).filter(Boolean))];
    if (normalizedSymbols.length === 0) return;
    if (normalizedSymbols.length === 1) {
      await this.connection.invoke('UnsubscribeFromSymbol', normalizedSymbols[0]);
    } else {
      await this.connection.invoke('UnsubscribeFromSymbols', normalizedSymbols);
    }
    console.log('[SignalR] Unsubscribed from symbols', normalizedSymbols);

    this.currentSymbols = this.currentSymbols.filter((symbol) => !normalizedSymbols.includes(symbol));
    if (this.currentSymbols.length === 0) {
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
