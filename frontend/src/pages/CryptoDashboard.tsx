import React from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Column, DataTable } from '../components/common/DataTable';
import { useCryptoInstruments } from '../hooks/useCryptoInstruments';
import './MarketDashboard.css';

export interface Crypto {
  id: string;
  symbol: string;
  name: string;
  description: string;
  baseCurrency: string;
  quoteCurrency: string;
  status: string;
  price?: number;
  change?: number;
  changePercent?: number;
  volume24h?: number;
  marketCap?: number;
  dominance?: number;
}

export const CryptoDashboard: React.FC = () => {
  const navigate = useNavigate();
  const { crypto, loading, error } = useCryptoInstruments();

  const columns: Column<Crypto>[] = [
    {
      key: 'symbol',
      label: 'Symbol',
      width: '15%',
      render: (value: string, item: Crypto) => (
        <Link className="crypto-link" to={`/dashboard/crypto/${item.symbol}`}>
          <strong>{value}</strong>
        </Link>
      ),
    },
    {
      key: 'name',
      label: 'Nazwa',
      width: '30%',
    },
    {
      key: 'baseCurrency',
      label: 'Waluta Bazowa',
      width: '12%',
      render: (value: string) => <span>{value || 'N/A'}</span>,
    },
    {
      key: 'quoteCurrency',
      label: 'Waluta Kwotowania',
      width: '12%',
      render: (value: string) => <span>{value || 'N/A'}</span>,
    },
    {
      key: 'status',
      label: 'Status',
      width: '12%',
      render: (value: string) => (
        <span className={`status-badge status-${value?.toLowerCase()}`}>
          {value || 'Unknown'}
        </span>
      ),
    },
    {
      key: 'description',
      label: 'Opis',
      width: '19%',
      render: (value: string) => <span>{value || '--'}</span>,
    },
  ];

  if (loading) {
    return (
      <div className="market-dashboard">
        <div className="dashboard-header">
          <h1>₿ Crypto Dashboard</h1>
          <p className="subtitle">Kryptowaluty - Rynek 24/7</p>
        </div>
        <div className="dashboard-content">
          <div className="loader">Wczytywanie danych...</div>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="market-dashboard">
        <div className="dashboard-header">
          <h1>₿ Crypto Dashboard</h1>
          <p className="subtitle">Kryptowaluty - Rynek 24/7</p>
        </div>
        <div className="dashboard-content">
          <div className="error-message">
            ❌ Błąd podczas wczytywania danych: {error.message}
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="market-dashboard">
      <div className="dashboard-header">
        <h1>₿ Crypto Dashboard</h1>
        <p className="subtitle">Kryptowaluty - Rynek 24/7</p>
      </div>

      <div className="dashboard-content">
        <div className="datatable-container">
          <DataTable<Crypto>
            columns={columns}
            data={crypto as unknown as Crypto[]}
            keyExtractor={(item) => item.id}
            emptyMessage="Brak dostępnych kryptowalut"
            onRowClick={(item) => navigate(`/dashboard/crypto/${item.symbol}`)}
          />
        </div>
      </div>
    </div>
  );
};
