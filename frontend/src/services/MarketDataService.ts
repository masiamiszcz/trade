/**
 * Unified Market Data Service
 * Handles all market data operations (health checks, assets, instruments, etc.)
 * 
 * Uses centralized HTTP client with interceptors,
 * proper error handling, and request logging
 */

import { httpClient } from './http/HttpClient';
import { ApiError, getErrorMessage } from './http/ApiError';
import { API_CONFIG } from '../config/apiConfig';
import type { HealthStatus, MarketAsset } from '../types';

/**
 * Instrument type
 */
export interface Instrument {
  id: string;
  name: string;
  symbol: string;
  type: string;
}

/**
 * Unified Market Data Service
 */
class MarketDataService {
  /**
   * Get health status
   */
  async getHealth(): Promise<HealthStatus> {
    try {
      return await httpClient.fetch<HealthStatus>({
        url: API_CONFIG.endpoints.market.health,
        method: 'GET',
      });
    } catch (error) {
      throw this.handleError(error);
    }
  }

  /**
   * Get all market assets
   */
  async getAllAssets(): Promise<MarketAsset[]> {
    try {
      return await httpClient.fetch<MarketAsset[]>({
        url: API_CONFIG.endpoints.market.assets,
        method: 'GET',
      });
    } catch (error) {
      throw this.handleError(error);
    }
  }

  /**
   * Get specific asset by symbol
   */
  async getAssetBySymbol(symbol: string): Promise<MarketAsset> {
    try {
      return await httpClient.fetch<MarketAsset>({
        url: API_CONFIG.endpoints.market.asset(symbol),
        method: 'GET',
      });
    } catch (error) {
      throw this.handleError(error);
    }
  }

  /**
   * Get all instruments
   */
  async getInstruments(): Promise<Instrument[]> {
    try {
      return await httpClient.fetch<Instrument[]>({
        url: API_CONFIG.endpoints.market.instruments,
        method: 'GET',
      });
    } catch (error) {
      throw this.handleError(error);
    }
  }

  /**
   * Handle and normalize errors
   */
  private handleError(error: unknown): Error {
    if (error instanceof ApiError) {
      console.error('Market Data API Error:', error.getDetails());
      return error;
    }

    if (error instanceof Error) {
      return error;
    }

    return new Error(getErrorMessage(error));
  }
}

// Export singleton instance
export const marketDataService = new MarketDataService();

// Log initialization in development
if (process.env.NODE_ENV === 'development') {
  console.log('%c✓ Market Data Service initialized', 'color: #00aa00; font-weight: bold');
}

export default marketDataService;
