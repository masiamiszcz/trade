
import React, { useState, useEffect } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { adminAuthService } from '../services/AdminAuthService';
import { useAdminAuth } from '../hooks/admin/useAdminAuth';
import { TwoFactorInput } from '../components/admin/2FA/TwoFactorInput';
import { AdminVerify2FARequest } from '../types/adminAuth';

interface LocationState {
  sessionId: string;
  username: string;
}

export const AdminVerify2FAPage: React.FC = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const { token, isTempToken, setSession, clearSession, isAuthenticated } = useAdminAuth();
  const state = location.state as LocationState | null;

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string>('');
  const [attempts, setAttempts] = useState(0);

  // Redirect to dashboard when authenticated (after setSession completes)
  useEffect(() => {
    if (isAuthenticated && !loading) {
      navigate('/admin/dashboard', { replace: true });
    }
  }, [isAuthenticated, loading, navigate]);

  // Protect this page - must have temp token and sessionId
  if (!token || !isTempToken || !state) {
    navigate('/admin/login');
    return null;
  }

  const handleCodeSubmit = async (code: string) => {
    setError('');
    setLoading(true);

    try {
      const request: AdminVerify2FARequest = {
        sessionId: state.sessionId,
        code,
      };

      const result = await adminAuthService.adminVerify2FA(request, token!);

      if (result.error) {
        const newAttempts = attempts + 1;
        setAttempts(newAttempts);

        if (newAttempts >= 3) {
          setError('Zbyt wiele nieudanych prób. Zaloguj się ponownie.');
          setTimeout(() => {
            clearSession();
            navigate('/admin/login');
          }, 2000);
        } else {
          setError(
            `Błędny kod. (Próba ${newAttempts}/3) ${result.error.message || 'Spróbuj ponownie'}`
          );
        }
        setLoading(false);
        return;
      }

      if (!result.data) {
        setError('Nieznany błąd');
        setLoading(false);
        return;
      }

      // ✅ UNIFIED: Save to trading-admin-session (main storage key)
      // This ensures hook finds token immediately after 2FA verification
      const sessionData = {
        token: result.data.token,
        sessionId: result.data.adminId,
        adminId: result.data.adminId,
        username: result.data.username,
        isTempToken: false,
        requiresTwoFactor: false,
      };
      
      // Direct localStorage save to ensure immediate availability
      localStorage.setItem('trading-admin-session', JSON.stringify(sessionData));
      console.log('✅ Token saved to trading-admin-session (direct localStorage)');
      
      // Also update context (for React state sync)
      setSession(sessionData);
      setLoading(false);
    } catch (err) {
      setError('Nieznany błąd - sprawdź konsolę');
      console.error('2FA verification error:', err);
      setLoading(false);
    }
  };

  return (
    <div className="auth-page">
      <div className="auth-card admin-card verify-2fa-card">
        <div className="admin-badge">🔐 WERYFIKACJA 2FA</div>
        <h1>Weryfikacja Dwustopniowa</h1>
        <p className="admin-subtitle">Wpisz kod z Authenticator'a</p>

        {error && (
          <div className={`${attempts >= 3 ? 'error-message critical' : 'error-message'}`}>
            {error}
          </div>
        )}

        <div className="login-info">
          <p>Zalogowany jako: <strong>{state.username}</strong></p>
        </div>

        <TwoFactorInput
          onCodeSubmit={handleCodeSubmit}
          isLoading={loading}
          error={error ? '' : undefined}
          label="6-cyfrowy kod z Authenticator'a lub kod zapasowy"
        />

        <div className="info-box">
          <p>
            💡 Wpisz 6-cyfrowy kod z aplikacji Authenticator lub użyj kodu zapasowego jeśli straciłeś dostęp do aplikacji.
          </p>
        </div>

        <div className="attempts-indicator">
          <p>Pozostałe próby: <strong>{3 - attempts}/3</strong></p>
          <div className="attempts-bar">
            <div
              className="attempts-filled"
              style={{ width: `${((3 - attempts) / 3) * 100}%` }}
            ></div>
          </div>
        </div>

        <button
          type="button"
          className="btn-link"
          onClick={() => {
            clearSession();
            navigate('/admin/login');
          }}
        >
          ← Wróć do logowania
        </button>
      </div>
    </div>
  );
};
