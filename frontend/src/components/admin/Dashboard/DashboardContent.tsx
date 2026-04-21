import React, { useState, useEffect } from 'react';
import { useAdminRequests } from '../../../hooks/admin/useAdminRequests';
import './DashboardContent.css';

interface HealthStatus {
  status: string;
  message?: string;
  timestamp?: string;
}

export const DashboardContent: React.FC = () => {
  const { requests, loading: requestsLoading } = useAdminRequests();
  const [health, setHealth] = useState<HealthStatus | null>(null);
  const [healthLoading, setHealthLoading] = useState(true);

  useEffect(() => {
    const fetchHealth = async () => {
      try {
        const response = await fetch('/api/health');
        const data = await response.json();
        setHealth(data);
      } catch (error) {
        console.error('Health check failed:', error);
        setHealth({ status: 'Unavailable', message: 'Nie można połączyć z serwerem' });
      } finally {
        setHealthLoading(false);
      }
    };

    fetchHealth();
    const interval = setInterval(fetchHealth, 30000); // Refresh every 30 seconds
    return () => clearInterval(interval);
  }, []);

  const pendingRequests = requests.filter((r) => r.status === 'pending').length;
  const approvedRequests = requests.filter((r) => r.status === 'approved').length;
  const rejectedRequests = requests.filter((r) => r.status === 'rejected').length;

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
            {healthLoading ? (
              <p className="loading">Sprawdzanie...</p>
            ) : health ? (
              <>
                <div className={`health-status ${health.status.toLowerCase()}`}>
                  {health.status === 'Healthy' || health.status === 'OK' ? '🟢' : '🔴'} {health.status}
                </div>
                {health.message && <p className="health-message">{health.message}</p>}
                {health.timestamp && (
                  <p className="health-info">
                    <strong>Ostatnia aktualizacja:</strong> {new Date(health.timestamp).toLocaleTimeString('pl-PL')}
                  </p>
                )}
              </>
            ) : (
              <p className="error">Nie udało się sprawdzić statusu</p>
            )}
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
