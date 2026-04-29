import React, { useMemo } from 'react';
import { Link, useParams } from 'react-router-dom';
import { useCryptoInstruments } from '../hooks/useCryptoInstruments';
import { useSignalR } from '../hooks/useSignalR';
import './CryptoPage.css';

export const CryptoPage: React.FC = () => {
  const { symbol } = useParams<{ symbol: string }>();
  const { crypto, loading, error } = useCryptoInstruments();

  const instrument = useMemo(
    () => crypto.find((item) => item.symbol.toUpperCase() === symbol?.toUpperCase()),
    [crypto, symbol]
  );

  const accessTokenFactory = useMemo(
    () => () => localStorage.getItem('auth_token'),
    []
  );

  const { latestPrice, connectionState, error: signalRError } = useSignalR({
    symbol: symbol?.toUpperCase() ?? '',
    accessTokenFactory,
  });

  return (
    <div className="crypto-page">
      <div className="crypto-page-header">
        <h1>Crypto details</h1>
        <p className="subtitle">Live updates and quote feed for {symbol?.toUpperCase()}</p>
        <Link className="back-link" to="/dashboard/crypto">
          ← Powrót do listy kryptowalut
        </Link>
      </div>

      <div className="crypto-page-content">
        {loading ? (
          <div className="loader">Ładowanie instrumentu...</div>
        ) : error ? (
          <div className="error-message">Błąd: {error.message}</div>
        ) : !instrument ? (
          <div className="error-message">Nie znaleziono instrumentu {symbol}</div>
        ) : (
          <>
            <div className="instrument-summary">
              <div className="instrument-card">
                <div className="instrument-title">{instrument.name}</div>
                <div className="instrument-symbol">{instrument.symbol}</div>
                <div className="instrument-meta">
                  <span>Base: {instrument.baseCurrency}</span>
                  <span>Quote: {instrument.quoteCurrency}</span>
                  <span>Status: {instrument.status}</span>
                </div>
                <div className="instrument-description">{instrument.description || 'Brak opisu'}</div>
              </div>

              <div className="live-summary">
                <div className="live-box">
                  <div className="live-label">Połączenie SignalR</div>
                  <div className={`live-status live-${connectionState}`}>
                    {connectionState}
                  </div>
                </div>
                <div className="live-box">
                  <div className="live-label">Ostatnia cena</div>
                  <div className="live-value">
                    {latestPrice ? `${latestPrice.price.toFixed(2)} ${instrument.quoteCurrency}` : 'Brak danych' }
                  </div>
                </div>
                <div className="live-box">
                  <div className="live-label">Ostatni update</div>
                  <div className="live-value">
                    {latestPrice ? new Date(latestPrice.timestamp).toLocaleTimeString() : '—'}
                  </div>
                </div>
                {signalRError && <div className="error-message">SignalR: {signalRError}</div>}
              </div>
            </div>

            <div className="crypto-note">
              Po wejściu na tę stronę uruchomiono połączenie SignalR dla symbolu {instrument.symbol}. Tutaj możesz rozbudować widok o wykresy i historię ticków.
            </div>
          </>
        )}
      </div>
    </div>
  );
};
