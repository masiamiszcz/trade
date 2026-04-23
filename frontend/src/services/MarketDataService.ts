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
import type { Instrument as AdminInstrument } from '../types/admin';
import type { HealthStatus, MarketAsset } from '../types';

/**
 * Instrument type - basic market instrument
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
        url: API_CONFIG.endpoints.health.public,
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
        url: API_CONFIG.endpoints.market.all,
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
        url: API_CONFIG.endpoints.market.bySymbol(symbol),
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
        url: API_CONFIG.endpoints.instruments.all,
        method: 'GET',
      });
    } catch (error) {
      throw this.handleError(error);
    }
  }

  /**
   * Get available instruments for users (active, non-blocked, approved)
   * Endpoint: GET /api/instruments/active
   * Returns only instruments that are:
   * - Status: Approved
   * - isBlocked: false
   * - isActive: true
   */
  async getAvailableInstruments(): Promise<AdminInstrument[]> {
    try {
      return await httpClient.fetch<AdminInstrument[]>({
        url: API_CONFIG.endpoints.instruments.active,
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
