import React from 'react';
import { PortfolioGrid } from '../components/user/PortfolioGrid';
import './DashboardPage.css';

export const DashboardPage: React.FC = () => {
  return (
    <div className="dashboard-page">
      <div className="dashboard-container">
        {/* Portfolio Grid Section */}
        <div className="dashboard-section portfolio-section">
          <h2 className="section-title">Twoje Portfolio</h2>
          <PortfolioGrid />
        </div>
      </div>
    </div>
  );
};
