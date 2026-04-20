import React, { useState, useEffect } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { authService } from '../services/AuthenticationService';
import { useAuth } from '../hooks/useAuth';
import { UserRegisterComplete2FARequest } from '../types/userAuth';
import { QRCodeDisplay, BackupCodesModal, TwoFactorInput } from '../components/shared/2FA';

interface LocationState {
  qrCodeDataUrl: string;
  manualKey: string;
  backupCodes: string[];
  message: string;
}

export const UserSetup2FAPage: React.FC = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const auth = useAuth();
  const state = location.state as LocationState | null;

  const [showManualKey, setShowManualKey] = useState(false);
  const [showBackupCodesModal, setShowBackupCodesModal] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string>('');
  const [backupCodes, setBackupCodes] = useState<string[]>([]);

  // ✅ Redirect logic przeniesiony do useEffect
  useEffect(() => {
    if (!auth.tempToken || !auth.sessionId) {
      navigate('/register', { replace: true });
    }
  }, [auth.tempToken, auth.sessionId, navigate]);

  useEffect(() => {
    if (!state || !state.manualKey) {
      navigate('/register', { replace: true });
    }
  }, [state, navigate]);

  // ✅ Guard dla TypeScript i renderu
  if (!state || !state.manualKey) {
    return null;
  }

  const handleCodeSubmit = async (code: string) => {
    setError('');
    setLoading(true);

    try {
      const request: UserRegisterComplete2FARequest = {
        sessionId: auth.sessionId!,
        code,
      };

      const response = await authService.userRegisterComplete2FA(request, auth.tempToken!);

      if (response.backupCodes && response.backupCodes.length > 0) {
        setBackupCodes(response.backupCodes);
        setShowBackupCodesModal(true);
      } else {
        auth.clearTempSession();
        navigate('/login', { replace: true });
      }
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Błąd przy weryfikacji kodu';
      setError(message);
      setLoading(false);
    }
  };

  const handleBackupCodesConfirm = () => {
    auth.clearTempSession();
    navigate('/login', { replace: true });
  };

  return (
    <div className="auth-page">
      <div className="auth-card">
        <h1>Konfiguracja Dwustopniowej Weryfikacji</h1>
        <p className="auth-subtitle">Skanuj kod QR aby włączyć 2FA</p>

        {error && <div className="error-message">{error}</div>}

        {!showBackupCodesModal ? (
          <>
            <QRCodeDisplay
              manualKey={state.manualKey}
              showManualKey={showManualKey}
              onToggleManualKey={() => setShowManualKey(!showManualKey)}
            />

            <div className="divider">lub</div>

            <div className="code-section">
              <h3>Po scanie - Wpisz kod z aplikacji</h3>
              <TwoFactorInput
                onCodeSubmit={handleCodeSubmit}
                isLoading={loading}
                error={error}
                label="6-cyfrowy kod z Authenticator'a"
              />
            </div>

            <div className="info-box">
              <p>
                ℹ️ Po skanowaniu QR kodem aplikacją (Google Authenticator, Microsoft Authenticator, Authy),
                wpisz 6-cyfrowy kod który się pojawił.
              </p>
            </div>
          </>
        ) : (
          <BackupCodesModal
            backupCodes={backupCodes}
            onConfirm={handleBackupCodesConfirm}
            isOpen={true}
          />
        )}
      </div>
    </div>
  );
};