import React, { useState } from 'react';
import { PortfolioPanel } from '../components/crypto/PortfolioPanel';
import { TransactionHistory } from '../components/crypto/TransactionHistory';
import './CryptoPortfolioPage.css';

export const CryptoPortfolioPage: React.FC = () => {
  const [activeTab, setActiveTab] = useState<'portfolio' | 'history'>('portfolio');

  return (
    <div className="crypto-portfolio-page">
      <header className="page-header">
        <h1>Crypto Portfolio</h1>
        <p>Manage your cryptocurrency holdings and trading history</p>
      </header>

      <div className="tabs">
        <button
          className={`tab-btn ${activeTab === 'portfolio' ? 'active' : ''}`}
          onClick={() => setActiveTab('portfolio')}
        >
          Portfolio
        </button>
        <button
          className={`tab-btn ${activeTab === 'history' ? 'active' : ''}`}
          onClick={() => setActiveTab('history')}
        >
          Transaction History
        </button>
      </div>

      <div className="tab-content">
        {activeTab === 'portfolio' && (
          <PortfolioPanel onRefresh={() => console.log('Portfolio refreshed')} />
        )}
        {activeTab === 'history' && (
          <TransactionHistory limit={50} />
        )}
      </div>
    </div>
  );
};
