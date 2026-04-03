// API Response types
export interface HealthStatus {
  status: 'Healthy' | 'Unhealthy';
  isReady: boolean;
  databaseHealthy: boolean;
  message: string;
  timestamp: string;
}

export interface MarketAsset {
  symbol: string;
  name: string;
  price: number;
  currency: string;
  changePercent: number;
}

// API Error type
export interface ApiError {
  message: string;
  status?: number;
}

// Generic API Response
export interface ApiResponse<T> {
  data?: T;
  error?: ApiError;
}