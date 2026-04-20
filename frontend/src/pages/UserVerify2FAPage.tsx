import React, { useState, useEffect } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { authService } from '../services/AuthenticationService';
import { useAuth } from '../hooks/useAuth';
import { UserVerifyLogin2FARequest } from '../types/userAuth';
import { TwoFactorInput } from '../components/shared/2FA';

interface LocationState {
  sessionId: string;
  username: string;
}

export const UserVerify2FAPage: React.FC = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const auth = useAuth();
  const state = location.state as LocationState | null;

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string>('');
  const [attempts, setAttempts] = useState(0);

  // Redirect to dashboard when authenticated (after setSession completes)
  useEffect(() => {
    if (auth.isAuthenticated && !loading) {
      navigate('/dashboard', { replace: true });
    }
  }, [auth.isAuthenticated, loading, navigate]);

  if (!auth.tempToken || !auth.requires2FA || !state) {
    navigate('/user/login', { replace: true });
    return null;
  }

  const handleCodeSubmit = async (code: string) => {
    setError('');
    setLoading(true);

    try {
      const request: UserVerifyLogin2FARequest = {
        sessionId: auth.sessionId!,
        code,
      };

      const response = await authService.userVerifyLogin2FA(request);

      // Token już był ustawiony w AuthenticationService.userVerifyLogin2FA
      // Wyczyść temp session
      auth.clearTempSession();
      setLoading(false);
    } catch (err) {
      const newAttempts = attempts + 1;
      setAttempts(newAttempts);

      const message = err instanceof Error ? err.message : 'Błąd weryfikacji kodu';

      if (newAttempts >= 3) {
        setError('Zbyt wiele nieudanych prób. Zaloguj się ponownie.');
        setTimeout(() => {
          auth.clearTempSession();
          navigate('/user/login', { replace: true });
        }, 2000);
      } else {
        setError(`Błędny kod. (Próba ${newAttempts}/3) ${message}`);
      }
      setLoading(false);
    }
  };

  return (
    <div className="auth-page">
      <div className="auth-card">
        <h1>Weryfikacja Dwustopniowa</h1>
        <p className="auth-subtitle">Wpisz kod z Authenticator'a</p>
        <p className="auth-hint">Zalogowany jako: <strong>{state.username}</strong></p>

        {error && (
          <div className={`${attempts >= 3 ? 'error-message critical' : 'error-message'}`}>
            {error}
          </div>
        )}

        <TwoFactorInput
          onCodeSubmit={handleCodeSubmit}
          isLoading={loading}
          error={error}
          label="6-cyfrowy kod z Authenticator'a"
        />

        <div className="info-box">
          <p>
            ℹ️ Wpisz 6-cyfrowy kod z aplikacji Authenticator. Kod zostanie weryfikowany automatycznie.
          </p>
        </div>

        <div className="auth-footer">
          <p>
            <button
              type="button"
              className="btn-link"
              onClick={() => {
                auth.clearTempSession();
                navigate('/user/login', { replace: true });
              }}
              disabled={loading}
            >
              Zaloguj się ponownie
            </button>
          </p>
        </div>
      </div>
    </div>
  );
};
