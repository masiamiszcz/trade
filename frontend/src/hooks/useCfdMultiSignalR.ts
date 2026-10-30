import { useCallback, useEffect, useRef, useState } from 'react';
import { PriceUpdate, SignalRService } from '../services/SignalRService';

type ConnectionState = 'connected' | 'connecting' | 'disconnected';

interface UseCfdMultiSignalROptions {
  symbols: string[];
  rangeMinutes: number;
  accessTokenFactory: () => string | null;
  onPriceUpdate: (update: PriceUpdate) => void;
}

export function useCfdMultiSignalR({
  symbols,
  rangeMinutes,
  accessTokenFactory,
  onPriceUpdate,
}: UseCfdMultiSignalROptions) {
  const [latestPrices, setLatestPrices] = useState<Record<string, PriceUpdate>>({});
  const [connectionState, setConnectionState] = useState<ConnectionState>('disconnected');
  const [error, setError] = useState<string | null>(null);
  const serviceRef = useRef<SignalRService | null>(null);
  const callbackRef = useRef(onPriceUpdate);
  const symbolsKey = [...new Set(symbols.map((symbol) => symbol.toUpperCase()))].join('|');
  const selectedSymbolsRef = useRef(new Set(symbolsKey ? symbolsKey.split('|') : []));
  callbackRef.current = onPriceUpdate;
  selectedSymbolsRef.current = new Set(symbolsKey ? symbolsKey.split('|') : []);

  const handlePriceUpdate = useCallback((update: PriceUpdate) => {
    const symbol = update.symbol.toUpperCase();
    if (!selectedSymbolsRef.current.has(symbol)) return;
    setLatestPrices((previous) => ({ ...previous, [symbol]: update }));
    callbackRef.current(update);
  }, []);

  useEffect(() => {
    if (!symbolsKey) {
      setConnectionState('disconnected');
      setLatestPrices({});
      return;
    }

    let active = true;
    const normalizedSymbols = symbolsKey ? symbolsKey.split('|') : [];
    setLatestPrices((previous) => Object.fromEntries(
      Object.entries(previous).filter(([symbol]) => normalizedSymbols.includes(symbol)),
    ));
    setConnectionState('connecting');
    setError(null);

    const service = new SignalRService({
      accessTokenFactory,
      onPriceUpdate: handlePriceUpdate,
      onConnected: () => {
        if (!active) return;
        setConnectionState('connected');
        setError(null);
      },
      onDisconnected: (disconnectError) => {
        if (!active) return;
        setConnectionState('disconnected');
        setError(disconnectError ? String(disconnectError) : null);
      },
    });
    serviceRef.current = service;

    void service.connect()
      .then(async () => {
        if (!active) return;
        if (!service.isConnected()) {
          setConnectionState('disconnected');
          return;
        }
        await service.subscribeToSymbols(normalizedSymbols, rangeMinutes);
      })
      .catch((connectionError: unknown) => {
        if (!active) return;
        setConnectionState('disconnected');
        setError(connectionError instanceof Error ? connectionError.message : String(connectionError));
      });

    return () => {
      active = false;
      if (serviceRef.current === service) serviceRef.current = null;
      void service.disconnect();
    };
  }, [accessTokenFactory, handlePriceUpdate, rangeMinutes, symbolsKey]);

  return { latestPrices, connectionState, error };
}
