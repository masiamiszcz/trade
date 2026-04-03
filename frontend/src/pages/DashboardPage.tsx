import React from 'react';


export const DashboardPage: React.FC = () => {
  return (
    <div className="dashboard-page">
      <div className="dashboard-header">
        <h1>Dashboard</h1>
        <p>Witaj w aplikacji Trading Platform</p>
      </div>

      <div className="dashboard-grid">
        <div className="dashboard-card">
          <h3>Portfolio</h3>
          <p>Zarządzaj swoim portfelem inwestycyjnym</p>
          <div className="card-placeholder">
            <span>Portfolio Component</span>
          </div>
        </div>

        <div className="dashboard-card">
          <h3>Rynek</h3>
          <p>Śledź aktualne ceny instrumentów</p>
          <div className="card-placeholder">
            <span>Market Component</span>
          </div>
        </div>

        <div className="dashboard-card">
          <h3>Transakcje</h3>
          <p>Historia Twoich transakcji</p>
          <div className="card-placeholder">
            <span>Transactions Component</span>
          </div>
        </div>

        <div className="dashboard-card">
          <h3>Analizy</h3>
          <p>Narzędzia analityczne i wykresy</p>
          <div className="card-placeholder">
            <span>Analytics Component</span>
          </div>
        </div>
      </div>
    </div>
  );
};
