import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { PriceUpdate, SignalRService } from '../services/SignalRService';

export interface UseSignalROptions {
  symbol: string;
  onPriceUpdate?: (update: PriceUpdate) => void;
  accessTokenFactory?: () => string | null | Promise<string | null>;
}

export interface UseSignalRResult {
  latestPrice: PriceUpdate | null;
  connectionState: 'connected' | 'connecting' | 'disconnected';
  error: string | null;
  subscribe: () => Promise<void>;
  unsubscribe: () => Promise<void>;
  disconnect: () => Promise<void>;
}

export const useSignalR = ({ symbol, onPriceUpdate, accessTokenFactory }: UseSignalROptions): UseSignalRResult => {
  const [latestPrice, setLatestPrice] = useState<PriceUpdate | null>(null);
  const [connectionState, setConnectionState] = useState<'connected' | 'connecting' | 'disconnected'>('disconnected');
  const [error, setError] = useState<string | null>(null);

  const serviceRef = useRef<SignalRService | null>(null);
  const accessTokenRef = useRef(accessTokenFactory);
  const onPriceUpdateRef = useRef(onPriceUpdate);

  accessTokenRef.current = accessTokenFactory;
  onPriceUpdateRef.current = onPriceUpdate;

  const handlePriceUpdate = useCallback((update: PriceUpdate) => {
    setLatestPrice(update);
    onPriceUpdateRef.current?.(update);
  }, []);

  const initializeService = useCallback(() => {
    if (!serviceRef.current) {
      serviceRef.current = new SignalRService({
        accessTokenFactory: () => accessTokenRef.current?.() ?? null,
        onPriceUpdate: handlePriceUpdate,
        onConnected: () => {
          setConnectionState('connected');
          setError(null);
        },
        onDisconnected: (disconnectError) => {
          setConnectionState('disconnected');
          setError(disconnectError ? String(disconnectError) : null);
        },
      });
    }

    return serviceRef.current;
  }, [handlePriceUpdate]);

  const connect = useCallback(async () => {
    setConnectionState('connecting');
    const service = initializeService();
    try {
      await service.connect();
      await service.subscribeToSymbol(symbol);
    } catch (connectError) {
      setConnectionState('disconnected');
      setError(connectError instanceof Error ? connectError.message : String(connectError));
    }
  }, [initializeService, symbol]);

  const unsubscribe = useCallback(async () => {
    const service = serviceRef.current;
    if (!service) return;

    try {
      await service.unsubscribeFromSymbol(symbol);
    } catch (unsubscribeError) {
      console.warn('[useSignalR] unsubscribe failed', unsubscribeError);
    }
  }, [symbol]);

  const disconnect = useCallback(async () => {
    const service = serviceRef.current;
    if (!service) return;
    await service.disconnect();
  }, []);

  useEffect(() => {
    connect();
    return () => {
      serviceRef.current?.disconnect();
    };
  }, [connect]);

  return {
    latestPrice,
    connectionState,
    error,
    subscribe: connect,
    unsubscribe,
    disconnect,
  };
};
