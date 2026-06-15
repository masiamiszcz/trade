import { useState, useCallback } from 'react';
import {
  cryptoPortfolioService,
  BuyCryptoRequest,
  SellCryptoRequest,
  CryptoTransactionDto,
} from '../services/CryptoPortfolioService';

interface UseBuySellState {
  data: CryptoTransactionDto | null;
  loading: boolean;
  error: string | null;
  success: boolean;
}

export function useBuySell(onSuccess?: () => void) {
  const [state, setState] = useState<UseBuySellState>({
    data: null,
    loading: false,
    error: null,
    success: false,
  });

  const buy = useCallback(async (request: BuyCryptoRequest) => {
    setState(prev => ({ ...prev, loading: true, error: null, success: false }));
    try {
      const transaction = await cryptoPortfolioService.buy(request);
      setState(prev => ({
        ...prev,
        data: transaction,
        loading: false,
        success: true,
      }));
      onSuccess?.();
      return transaction;
    } catch (error) {
      const errorMsg = error instanceof Error ? error.message : 'Buy operation failed';
      setState(prev => ({
        ...prev,
        error: errorMsg,
        loading: false,
        success: false,
      }));
      throw error;
    }
  }, [onSuccess]);

  const sell = useCallback(async (request: SellCryptoRequest) => {
    setState(prev => ({ ...prev, loading: true, error: null, success: false }));
    try {
      const transaction = await cryptoPortfolioService.sell(request);
      setState(prev => ({
        ...prev,
        data: transaction,
        loading: false,
        success: true,
      }));
      onSuccess?.();
      return transaction;
    } catch (error) {
      const errorMsg = error instanceof Error ? error.message : 'Sell operation failed';
      setState(prev => ({
        ...prev,
        error: errorMsg,
        loading: false,
        success: false,
      }));
      throw error;
    }
  }, [onSuccess]);

  const reset = useCallback(() => {
    setState({
      data: null,
      loading: false,
      error: null,
      success: false,
    });
  }, []);

  return {
    transaction: state.data,
    loading: state.loading,
    error: state.error,
    success: state.success,
    buy,
    sell,
    reset,
  };
}
