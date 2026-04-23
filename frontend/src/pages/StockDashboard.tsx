import React from 'react';
import { Column, DataTable } from '../components/common/DataTable';
import { useAvailableInstruments } from '../hooks/useAvailableInstruments';
import './MarketDashboard.css';

export interface Stock {
  id: string;
  symbol: string;
  name: string;
  price?: number;
  change?: number;
  changePercent?: number;
  volume?: number;
  marketCap?: number;
  peRatio?: number;
}

export const StockDashboard: React.FC = () => {
  const { stocks, loading, error } = useAvailableInstruments();

  const columns: Column<Stock>[] = [
    {
      key: 'symbol',
      label: 'Symbol',
      width: '15%',
      render: (value: string) => <strong>{value}</strong>,
    },
    {
      key: 'name',
      label: 'Nazwa',
      width: '30%',
    },
    {
      key: 'baseCurrency',
      label: 'Waluta',
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
      width: '31%',
      render: (value: string) => <span>{value || '--'}</span>,
    },
  ];

  if (loading) {
    return (
      <div className="market-dashboard">
        <div className="dashboard-header">
          <h1>📈 Stock Dashboard</h1>
          <p className="subtitle">Akcje - Giełda Papierów Wartościowych</p>
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
          <h1>📈 Stock Dashboard</h1>
          <p className="subtitle">Akcje - Giełda Papierów Wartościowych</p>
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
        <h1>📈 Stock Dashboard</h1>
        <p className="subtitle">Akcje - Giełda Papierów Wartościowych</p>
      </div>

      <div className="dashboard-content">
        <div className="datatable-container">
          <DataTable<Stock>
            columns={columns}
            data={stocks as unknown as Stock[]}
            keyExtractor={(item) => item.id}
            emptyMessage="Brak dostępnych akcji"
          />
        </div>
      </div>
    </div>
  );
};
