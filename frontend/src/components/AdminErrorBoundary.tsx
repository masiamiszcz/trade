

import React, { ReactNode, useState, useEffect } from 'react';

interface AdminErrorBoundaryProps {
  children: ReactNode;
}

interface AdminErrorBoundaryState {
  hasError: boolean;
  errorMessage: string;
}

export const AdminErrorBoundary: React.FC<AdminErrorBoundaryProps> = ({ children }) => {
  const [state, setState] = useState<AdminErrorBoundaryState>({
    hasError: false,
    errorMessage: '',
  });

  // Intercept fetch errors
  useEffect(() => {
    const originalFetch = window.fetch;

    window.fetch = async (...args: Parameters<typeof fetch>) => {
      const response = await originalFetch(...args);

      // Check if it's an admin route
      const url = typeof args[0] === 'string' ? args[0] : args[0].url;
      if (url.includes('/api/admin')) {
        if (response.status === 403) {
          setState({
            hasError: true,
            errorMessage:
              'Access Denied: You are not connected to the VPN. Please enable WireGuard and try again.',
          });
          return response.clone();
        }
      }

      return response;
    };

    return () => {
      window.fetch = originalFetch;
    };
  }, []);

  if (state.hasError) {
    return (
      <div className="admin-error-container" style={styles.container}>
        <div style={styles.card}>
          <div style={styles.icon}>🔒</div>
          <h1 style={styles.title}>Access Denied</h1>
          <p style={styles.message}>{state.errorMessage}</p>
          <div style={styles.steps}>
            <h3>To access Admin Panel:</h3>
            <ol>
              <li>Download WireGuard from https://www.wireguard.com/install/</li>
              <li>Open the WireGuard application</li>
              <li>Import your VPN configuration file</li>
              <li>Click "Connect"</li>
              <li>Refresh this page</li>
            </ol>
          </div>
          <button
            onClick={() => window.location.reload()}
            style={styles.button}
          >
            Retry
          </button>
        </div>
      </div>
    );
  }

  return <>{children}</>;
};

// Styles
const styles = {
  container: {
    display: 'flex',
    justifyContent: 'center',
    alignItems: 'center',
    minHeight: '100vh',
    backgroundColor: '#f5f5f5',
  } as React.CSSProperties,
  card: {
    backgroundColor: 'white',
    borderRadius: '8px',
    boxShadow: '0 2px 8px rgba(0,0,0,0.1)',
    padding: '40px',
    maxWidth: '600px',
    textAlign: 'center' as const,
  },
  icon: {
    fontSize: '64px',
    marginBottom: '16px',
  },
  title: {
    fontSize: '28px',
    fontWeight: 'bold',
    color: '#d32f2f',
    marginBottom: '16px',
  },
  message: {
    fontSize: '16px',
    color: '#666',
    marginBottom: '24px',
  },
  steps: {
    textAlign: 'left' as const,
    backgroundColor: '#f9f9f9',
    padding: '16px',
    borderRadius: '4px',
    marginBottom: '24px',
  },
  button: {
    backgroundColor: '#1976d2',
    color: 'white',
    border: 'none',
    padding: '12px 24px',
    borderRadius: '4px',
    fontSize: '16px',
    fontWeight: 'bold',
    cursor: 'pointer',
    transition: 'background-color 0.3s',
  },
};
