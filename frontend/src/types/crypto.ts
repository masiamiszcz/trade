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
}

export interface CryptoChartRequest {
  rangeMinutes: number;
  to?: string | null;
}
