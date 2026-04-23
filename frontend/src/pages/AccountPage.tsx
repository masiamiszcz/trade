import React from 'react';
import { useAccount } from '../hooks/useAccount';
import { TransactionHistory } from '../components/user/TransactionHistory';
import './AccountPage.css';

export const AccountPage: React.FC = () => {
  const { account, loading, error } = useAccount();

  return (
    <div className="account-page">
      <div className="account-container">
        {/* Account Card Section */}
        <div className="account-card-section">
          <div className="account-card">
            <h2>Twoje Konto</h2>
            {loading && <p className="loading-text">Ładowanie...</p>}
            {error && <p className="error-text">{error}</p>}
            {account ? (
              <>
                <p className="balance-label">Saldo</p>
                <h3 className="balance-amount">
                  {account.availableBalance.toLocaleString('pl-PL', {
                    minimumFractionDigits: 2,
                    maximumFractionDigits: 2,
                  })}
                </h3>
                <p className="currency-label">{account.currency}</p>
                <div className="account-details">
                  <div className="detail-item">
                    <span className="detail-label">Numer Konta:</span>
                    <span className="detail-value">{account.accountNumber}</span>
                  </div>
                  <div className="detail-item">
                    <span className="detail-label">Saldo Zarezerwowane:</span>
                    <span className="detail-value">
                      {account.reservedBalance.toLocaleString('pl-PL', {
                        minimumFractionDigits: 2,
                        maximumFractionDigits: 2,
                      })}
                    </span>
                  </div>
                </div>
              </>
            ) : (
              !loading && !error && <p className="loading-text">Brak danych konta.</p>
            )}
          </div>
        </div>

        {/* Transaction History Section */}
        <div className="transaction-section">
          <h2>Historia Transakcji</h2>
          <TransactionHistory />
        </div>
      </div>
    </div>
  );
};
