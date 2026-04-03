import { HealthStatus, MarketAsset, ApiResponse, ApiError } from '../types';

const API_BASE_URL = process.env.REACT_APP_API_URL || 'http://localhost/api';

class ApiService {
  private async request<T>(
    endpoint: string,
    options?: RequestInit
  ): Promise<ApiResponse<T>> {
    try {
      const response = await fetch(`${API_BASE_URL}${endpoint}`, {
        headers: {
          'Content-Type': 'application/json',
          ...options?.headers,
        },
        ...options,
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      const data = await response.json();
      return { data };
    } catch (error) {
      const apiError: ApiError = {
        message: error instanceof Error ? error.message : 'Unknown error occurred',
        status: error instanceof Response ? error.status : undefined,
      };
      return { error: apiError };
    }
  }

  // Health check endpoint
  async getHealth(): Promise<ApiResponse<HealthStatus>> {
    return this.request<HealthStatus>('/health');
  }

  // Get all market assets
  async getAllAssets(): Promise<ApiResponse<MarketAsset[]>> {
    return this.request<MarketAsset[]>('/market');
  }

  // Get specific asset by symbol
  async getAssetBySymbol(symbol: string): Promise<ApiResponse<MarketAsset>> {
    return this.request<MarketAsset>(`/market/${symbol}`);
  }
}

export const apiService = new ApiService();