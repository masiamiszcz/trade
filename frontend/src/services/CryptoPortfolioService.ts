import { httpClient } from './http/HttpClient';
import { ApiError, getErrorMessage } from './http/ApiError';
import { API_CONFIG } from '../config/apiConfig';

export interface BuyCryptoRequest {
  symbol: string;
  quantity: number;
  requestedPrice: number;
  tolerancePercent?: number;
}

export interface SellCryptoRequest {
  symbol: string;
  quantity: number;
  requestedPrice: number;
  tolerancePercent?: number;
}

export interface CryptoTransactionDto {
  id: string;
  symbol: string;
  type: string;
  quantity: number;
  requestedPrice: number;
  executionPrice: number;
  totalValue: number;
  status: string;
  currency: string;
  createdAt: string;
  completedAt?: string;
  errorMessage?: string;
}

export interface PortfolioPositionDto {
  symbol: string;
  quantity: number;
  averageOpenPrice: number;
  currentPrice: number;
  totalValue: number;
  pnL: number;
  pnLPercent: number;
}

export interface PortfolioAggregateDto {
  totalValue: number;
  totalPnL: number;
  totalPnLPercent: number;
}

export interface PortfolioSummaryDto {
  baseCurrency: string;
  positions: PortfolioPositionDto[];
  summary: PortfolioAggregateDto;
}

export interface PaginatedResult<T> {
  transactions: T[];
  total: number;
  limit: number;
  offset: number;
}

class CryptoPortfolioService {
  private handleError(error: unknown): Error {
    if (error instanceof ApiError) {
      // Extract errorCode from API response details
      const details = error.details as any;
      const errorCode = details?.errorCode;
      const errorMessage = details?.errorMessage;
      
      // Create user-friendly messages based on error code
      if (errorCode === 'PriceUnavailable') {
        return new Error('Cena rynkowa niedostępna. Spróbuj ponownie za kilka sekund.');
      }
      if (errorCode === 'PriceTolerance') {
        return new Error('Różnica ceny > 0.01%. Cena zmieniła się zbyt znacznie. Spróbuj ponownie.');
      }
      if (errorCode === 'InsufficientFunds' || errorCode === 'InsufficientBalance') {
        return new Error('Niewystarczające saldo. Sprawdź dostępne środki.');
      }
      if (errorCode === 'InsufficientPosition') {
        return new Error('Niewystarczająca ilość do sprzedania.');
      }
      if (errorCode === 'InvalidQuantity') {
        return new Error('Ilość musi być większa od zera.');
      }
      if (errorCode === 'InvalidPrice') {
        return new Error('Cena musi być większa od zera.');
      }
      if (errorCode === 'AccountNotFound') {
        return new Error('Konto nie znalezione. Odśwież stronę.');
      }
      
      // Fallback to error message from API
      return new Error(errorMessage || error.message || 'Błąd transakcji');
    }
    return error instanceof Error ? error : new Error('Unknown error');
  }

  async buy(request: BuyCryptoRequest): Promise<CryptoTransactionDto> {
    try {
      return await httpClient.fetch<CryptoTransactionDto>({
        url: API_CONFIG.endpoints.cryptoPortfolio.buy,
        method: 'POST',
        body: JSON.stringify(request),
      });
    } catch (error) {
      throw this.handleError(error);
    }
  }

  async sell(request: SellCryptoRequest): Promise<CryptoTransactionDto> {
    try {
      return await httpClient.fetch<CryptoTransactionDto>({
        url: API_CONFIG.endpoints.cryptoPortfolio.sell,
        method: 'POST',
        body: JSON.stringify(request),
      });
    } catch (error) {
      throw this.handleError(error);
    }
  }

  async getPortfolio(): Promise<PortfolioSummaryDto> {
    try {
      return await httpClient.fetch<PortfolioSummaryDto>({
        url: API_CONFIG.endpoints.cryptoPortfolio.portfolio,
        method: 'GET',
      });
    } catch (error) {
      throw this.handleError(error);
    }
  }

  async getTransactions(
    limit: number = 50,
    offset: number = 0,
    type?: string,
    status?: string
  ): Promise<PaginatedResult<CryptoTransactionDto>> {
    try {
      const params = new URLSearchParams();
      params.append('limit', limit.toString());
      params.append('offset', offset.toString());
      if (type) params.append('type', type);
      if (status) params.append('status', status);

      return await httpClient.fetch<PaginatedResult<CryptoTransactionDto>>({
        url: `${API_CONFIG.endpoints.cryptoPortfolio.transactions}?${params.toString()}`,
        method: 'GET',
      });
    } catch (error) {
      throw this.handleError(error);
    }
  }
}

export const cryptoPortfolioService = new CryptoPortfolioService();
