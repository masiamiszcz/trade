import React, { useState } from 'react';
import { useAccount } from '../hooks/useAccount';
import { PortfolioPanel } from '../components/crypto/PortfolioPanel';
import { TransactionHistory } from '../components/crypto/TransactionHistory';
import { TransactionHistory as UserTransactionHistory } from '../components/user/TransactionHistory';
import './AccountWithPortfolioPage.css';

export const AccountWithPortfolioPage: React.FC = () => {
  const { account, loading, error } = useAccount();
  const [activeTab, setActiveTab] = useState<'portfolio' | 'account'>('account');

  return (
    <div className="account-portfolio-page">
      {/* Secondary Navbar */}
      <nav className="portfolio-navbar">
        <div className="navbar-left">
          <button
            className={`navbar-btn ${activeTab === 'portfolio' ? 'active' : ''}`}
            onClick={() => setActiveTab('portfolio')}
          >
            <span className="navbar-icon">📊</span>
            AKTYWA
          </button>
        </div>
        <div className="navbar-right">
          <button
            className={`navbar-btn ${activeTab === 'account' ? 'active' : ''}`}
            onClick={() => setActiveTab('account')}
          >
            <span className="navbar-icon">💳</span>
            KONTA
          </button>
        </div>
      </nav>

      {/* Content Split */}
      <div className="content-container">
        {/* Left Section - Portfolio */}
        {activeTab === 'portfolio' && (
          <div className="content-section portfolio-section">
            <div className="section-header">
              <h2>Portfel Aktywów</h2>
              <p>Zarządzaj swoimi holdowniami kryptowalut</p>
            </div>
            <PortfolioPanel onRefresh={() => console.log('Portfolio refreshed')} />
            <div className="portfolio-history">
              <h3>Historia Transakcji</h3>
              <TransactionHistory limit={50} />
            </div>
          </div>
        )}

        {/* Right Section - Account */}
        {activeTab === 'account' && (
          <div className="content-section account-section">
            <div className="section-header">
              <h2>Twoje Konto</h2>
              <p>Informacje o koncie i historii transakcji</p>
            </div>

            {/* Account Card */}
            <div className="account-card">
              {loading && <p className="loading-text">Ładowanie...</p>}
              {error && <p className="error-text">{error}</p>}
              {account ? (
                <>
                  <div className="balance-section">
                    <p className="balance-label">Saldo Dostępne</p>
                    <h3 className="balance-amount">
                      {account.availableBalance.toLocaleString('pl-PL', {
                        minimumFractionDigits: 2,
                        maximumFractionDigits: 2,
                      })}
                    </h3>
                    <p className="currency-label">{account.currency}</p>
                  </div>
                  
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

            {/* Transaction History */}
            <div className="transaction-history-section">
              <h3>Historia Transakcji</h3>
              <UserTransactionHistory />
            </div>
          </div>
        )}
      </div>
    </div>
  );
};
