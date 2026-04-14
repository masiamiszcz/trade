import React from 'react';
import { useAccount } from '../hooks/useAccount';
import './DashboardPage.css';

export const DashboardPage: React.FC = () => {
  const { account, loading, error } = useAccount();

  return (
    <div className="dashboard-page">
      <div className="account-center">
        <div className="account-main-card">
          <h1>Twoje Konto</h1>
          {loading && <p className="loading-text">Ładowanie...</p>}
          {error && <p className="error-text">{error}</p>}
          {account ? (
            <>
              <p className="balance-label">Saldo</p>
              <h2 className="balance-amount">
                {account.availableBalance.toLocaleString('pl-PL', {
                  minimumFractionDigits: 2,
                  maximumFractionDigits: 2,
                })}
              </h2>
              <p className="currency-label">{account.currency}</p>
            </>
          ) : (
            !loading && !error && <p className="loading-text">Brak danych konta.</p>
          )}
        </div>
      </div>
    </div>
  );
};
