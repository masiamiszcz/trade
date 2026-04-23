import React from 'react';
import { Column, DataTable } from '../components/common/DataTable';
import { useAvailableInstruments } from '../hooks/useAvailableInstruments';
import './MarketDashboard.css';

export interface CFD {
  id: string;
  symbol: string;
  name: string;
  bid?: number;
  ask?: number;
  spread?: number;
  leverage?: number;
  dayHigh?: number;
  dayLow?: number;
  volume?: number;
}

export const CFDDashboard: React.FC = () => {
  const { cfd, loading, error } = useAvailableInstruments();

  const columns: Column<CFD>[] = [
    {
      key: 'symbol',
      label: 'Symbol',
      width: '12%',
      render: (value: string) => <strong>{value}</strong>,
    },
    {
      key: 'name',
      label: 'Nazwa Instrumentu',
      width: '25%',
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
      width: '27%',
      render: (value: string) => <span>{value || '--'}</span>,
    },
  ];

  if (loading) {
    return (
      <div className="market-dashboard">
        <div className="dashboard-header">
          <h1>💱 CFD Dashboard</h1>
          <p className="subtitle">Kontrakty CFD - Forex, Commodities, Indeksy</p>
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
          <h1>💱 CFD Dashboard</h1>
          <p className="subtitle">Kontrakty CFD - Forex, Commodities, Indeksy</p>
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
        <h1>💱 CFD Dashboard</h1>
        <p className="subtitle">Kontrakty CFD - Forex, Commodities, Indeksy</p>
      </div>

      <div className="dashboard-content">
        <div className="datatable-container">
          <DataTable<CFD>
            columns={columns}
            data={cfd as unknown as CFD[]}
            keyExtractor={(item) => item.id}
            emptyMessage="Brak dostępnych CFD"
          />
        </div>
      </div>
    </div>
  );
};
