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

export interface LoginRequest {
  userNameOrEmail: string;
  password: string;
}

export interface RegisterRequest {
  userName: string;
  email: string;
  firstName: string;
  lastName: string;
  password: string;
}

export interface AuthResponse {
  token: string;
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