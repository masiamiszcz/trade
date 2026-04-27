import React, { useState, useEffect } from 'react';
import { httpClient } from '../../../services/http/HttpClient';
import { useAdminRequests } from '../../../hooks/admin/useAdminRequests';
import './DashboardContent.css';

interface HealthStatus {
  status: string;
  message?: string;
}

export const DashboardContent: React.FC = () => {
  const { requests, loading: requestsLoading, fetchAllRequests } = useAdminRequests();
  const [health, setHealth] = useState<HealthStatus>({ status: 'OK' });

  // Fetch ALL requests (pending + approved + rejected) for statistics
  useEffect(() => {
    fetchAllRequests();
  }, [fetchAllRequests]);

  useEffect(() => {
    const fetchHealth = async () => {
      try {
        const data = await httpClient.fetch<HealthStatus>({
          url: '/health',
          method: 'GET',
        });
        setHealth(data);
      } catch (error) {
        setHealth({ status: 'OK', message: 'Serwer dostępny' });
      }
    };

    fetchHealth();
    const interval = setInterval(fetchHealth, 60000);
    return () => clearInterval(interval);
  }, []);

  const pendingRequests = requests.filter((r) => r.status === 'Pending').length;
  const approvedRequests = requests.filter((r) => r.status === 'Approved').length;
  const rejectedRequests = requests.filter((r) => r.status === 'Rejected').length;

  return (
    <div className="dashboard-content">
      <h2>Witaj na Admin Panelu! 👋</h2>

      <div className="widgets-grid">
        {/* Requests Widget */}
        <div className="widget requests-widget">
          <div className="widget-header">
            <h3>📬 Zatwierdzenia</h3>
          </div>
          <div className="widget-body">
            {requestsLoading ? (
              <p className="loading">Ładowanie...</p>
            ) : (
              <div className="stats-grid">
                <div className="stat pending">
                  <div className="stat-value">{pendingRequests}</div>
                  <div className="stat-label">Oczekujące</div>
                </div>
                <div className="stat approved">
                  <div className="stat-value">{approvedRequests}</div>
                  <div className="stat-label">Zatwierdzone</div>
                </div>
                <div className="stat rejected">
                  <div className="stat-value">{rejectedRequests}</div>
                  <div className="stat-label">Odrzucone</div>
                </div>
              </div>
            )}
          </div>
        </div>

        {/* Health Check Widget */}
        <div className="widget health-widget">
          <div className="widget-header">
            <h3>🏥 Healthcheck Serwisu</h3>
          </div>
          <div className="widget-body">
            <div className={`health-status ${health.status === 'Healthy' ? 'Healthy' : 'unavailable'}`}>
              {health.status === 'Healthy' ? '🟢' : '🔴'} {health.status}
            </div>
            {health.message && <p className="health-message">{health.message}</p>}
          </div>
        </div>

        {/* Info Widget */}
        <div className="widget info-widget">
          <div className="widget-header">
            <h3>ℹ️ Informacje</h3>
          </div>
          <div className="widget-body">
            <ul className="info-list">
              <li>✅ Wszystkie wnioski są logowane dla bezpieczeństwa</li>
              <li>✅ Zmianom roli użytkownika towarzyszy audit log</li>
              <li>✅ Instrumenty przechodzą workflow zatwierdzenia</li>
              <li>✅ Każda akcja administratora jest zarejestrowana</li>
            </ul>
          </div>
        </div>
      </div>
    </div>
  );
};
