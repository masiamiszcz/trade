export interface CryptoCandle {
  symbol: string;
  openTime: string;
  closeTime: string;
  open: number;
  high: number;
  low: number;
  close: number;
  volume: number;
  interval?: string;
  colorDirection?: 'up' | 'down';
}

export interface CryptoChartRequest {
  rangeMinutes: number;
  intervalMinutes?: number;
  to?: string | null;
  userBaseCurrency?: string;
}
