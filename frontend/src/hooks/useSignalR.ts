import { useCallback, useEffect, useRef, useState } from 'react';
import { CandleUpdate, PriceUpdate, SignalRService } from '../services/SignalRService';

export interface UseSignalROptions {
  symbol: string;
  rangeMinutes: number;
  onPriceUpdate?: (update: PriceUpdate) => void;
  onCandleUpdate?: (update: CandleUpdate) => void;
  accessTokenFactory?: () => string | null | Promise<string | null>;
}

export interface UseSignalRResult {
  latestPrice: PriceUpdate | null;
  connectionState: 'connected' | 'connecting' | 'disconnected';
  subscriptionState: 'idle' | 'subscribing' | 'subscribed' | 'unsubscribing';
  error: string | null;
  subscribe: () => Promise<void>;
  unsubscribe: () => Promise<void>;
  disconnect: () => Promise<void>;
}

export const useSignalR = ({ symbol, rangeMinutes, onPriceUpdate, onCandleUpdate, accessTokenFactory }: UseSignalROptions): UseSignalRResult => {
  const [latestPrice, setLatestPrice] = useState<PriceUpdate | null>(null);
  const [connectionState, setConnectionState] = useState<'connected' | 'connecting' | 'disconnected'>('disconnected');
  const [subscriptionState, setSubscriptionState] = useState<'idle' | 'subscribing' | 'subscribed' | 'unsubscribing'>('idle');
  const [error, setError] = useState<string | null>(null);

  const serviceRef = useRef<SignalRService | null>(null);
  const accessTokenRef = useRef(accessTokenFactory);
  const onPriceUpdateRef = useRef(onPriceUpdate);
  const onCandleUpdateRef = useRef(onCandleUpdate);
  const sequenceRef = useRef(0);
  const activeSequenceRef = useRef(0);

  accessTokenRef.current = accessTokenFactory;
  onPriceUpdateRef.current = onPriceUpdate;
  onCandleUpdateRef.current = onCandleUpdate;

  const handlePriceUpdate = useCallback((update: PriceUpdate) => {
    setLatestPrice(update);
    onPriceUpdateRef.current?.(update);
  }, []);

  const handleCandleUpdate = useCallback((update: CandleUpdate) => {
    onCandleUpdateRef.current?.(update);
  }, []);

  const initializeService = useCallback(() => {
    if (!serviceRef.current) {
      serviceRef.current = new SignalRService({
        accessTokenFactory: () => accessTokenRef.current?.() ?? null,
        onPriceUpdate: handlePriceUpdate,
        onCandleUpdate: handleCandleUpdate,
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
    if (!symbol) {
      return;
    }

    const requestId = ++sequenceRef.current;
    activeSequenceRef.current = requestId;

    setConnectionState('connecting');
    setSubscriptionState('subscribing');

    const service = initializeService();
    try {
      await service.connect();
      if (activeSequenceRef.current !== requestId) {
        return;
      }

      await service.subscribeToSymbol(symbol, rangeMinutes);
      if (activeSequenceRef.current !== requestId) {
        return;
      }

      setConnectionState('connected');
      setSubscriptionState('subscribed');
      setError(null);
    } catch (connectError) {
      if (activeSequenceRef.current !== requestId) {
        return;
      }

      setConnectionState('disconnected');
      setSubscriptionState('idle');
      setError(connectError instanceof Error ? connectError.message : String(connectError));
    }
  }, [initializeService, symbol, rangeMinutes]);

  const unsubscribe = useCallback(async () => {
    const requestId = ++sequenceRef.current;
    activeSequenceRef.current = requestId;

    const service = serviceRef.current;
    if (!service) return;

    setSubscriptionState('unsubscribing');

    try {
      await service.unsubscribeFromSymbol(symbol);
      if (activeSequenceRef.current !== requestId) {
        return;
      }

      setSubscriptionState('idle');
    } catch (unsubscribeError) {
      if (activeSequenceRef.current !== requestId) {
        return;
      }

      console.warn('[useSignalR] unsubscribe failed', unsubscribeError);
      setSubscriptionState('idle');
    }
  }, [symbol]);

  const disconnect = useCallback(async () => {
    const currentRequestId = ++sequenceRef.current;
    activeSequenceRef.current = currentRequestId;

    const service = serviceRef.current;
    if (!service) return;

    setSubscriptionState('idle');
    setConnectionState('disconnected');
    await service.disconnect();
  }, []);

  useEffect(() => {
    connect();
    return () => {
      unsubscribe();
    };
  }, [connect, unsubscribe]);

  useEffect(() => {
    return () => {
      disconnect();
    };
  }, [disconnect]);

  return {
    latestPrice,
    connectionState,
    subscriptionState,
    error,
    subscribe: connect,
    unsubscribe,
    disconnect,
  };
};
