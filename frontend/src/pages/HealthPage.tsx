import React from 'react';
import { useApi } from '../hooks/useApi';
import { apiService } from '../services/ApiService';
import { LoadingSpinner } from '../components/LoadingSpinner';
import { ErrorMessage } from '../components/ErrorMessage';
import { HealthStatus } from '../types';


export const HealthPage: React.FC = () => {
  const { data, loading, error, refetch } = useApi<HealthStatus>(
    () => apiService.getHealth()
  );

  if (loading) {
    return <LoadingSpinner message="Checking system health..." />;
  }

  if (error) {
    return (
      <ErrorMessage
        message={`Failed to check system health: ${error.message}`}
        onRetry={refetch}
      />
    );
  }

  return (
    <div className="health-page">
      <div className="health-header">
        <h1>System Health Status</h1>
        <button className="refresh-button" onClick={refetch}>
          Refresh
        </button>
      </div>

      {data && (
        <div className="health-card">
          <div className={`status-indicator ${data.status.toLowerCase()}`}>
            <span className="status-dot"></span>
            <span className="status-text">{data.status}</span>
          </div>

          <div className="health-details">
            <div className="detail-item">
              <span className="label">Application Ready:</span>
              <span className={`value ${data.isReady ? 'success' : 'error'}`}>
                {data.isReady ? 'Yes' : 'No'}
              </span>
            </div>

            <div className="detail-item">
              <span className="label">Database Healthy:</span>
              <span className={`value ${data.databaseHealthy ? 'success' : 'error'}`}>
                {data.databaseHealthy ? 'Yes' : 'No'}
              </span>
            </div>

            <div className="detail-item">
              <span className="label">Last Check:</span>
              <span className="value">
                {new Date(data.timestamp).toLocaleString()}
              </span>
            </div>
          </div>

          <div className="health-message">
            <p>{data.message}</p>
          </div>
        </div>
      )}
    </div>
  );
};
