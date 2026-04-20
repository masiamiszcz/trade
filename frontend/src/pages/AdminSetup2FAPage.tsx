
import React, { useState } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { adminAuthService } from '../services/AdminAuthService';
import { useAdminAuth } from '../hooks/admin/useAdminAuth';
import { QRCodeDisplay } from '../components/admin/2FA/QRCodeDisplay';
import { BackupCodesModal } from '../components/admin/2FA/BackupCodesModal';
import { TwoFactorInput } from '../components/admin/2FA/TwoFactorInput';
import { AdminSetup2FARequest } from '../types/adminAuth';

interface LocationState {
  manualKey: string;
  backupCodes?: string[];
}

export const AdminSetup2FAPage: React.FC = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const { session, setSession } = useAdminAuth();
  const state = location.state as LocationState | null;

  const [showManualKey, setShowManualKey] = useState(false);
  const [showBackupCodesModal, setShowBackupCodesModal] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string>('');
  const [backupCodes, setBackupCodes] = useState<string[]>([]);

  // Redirect if no temp token or no manual key
  if (!session.token) {
    navigate('/admin/register', { replace: true });
    return null;
  }

  if (!state || !state.manualKey) {
    navigate('/admin/register', { replace: true });
    return null;
  }

  const handleCodeSubmit = async (code: string) => {
    setError('');
    setLoading(true);

    try {
      const request: AdminSetup2FARequest = { code };
      const result = await adminAuthService.adminEnableSetup2FA(request, session.token!);

      if (result.error) {
        setError(result.error.message || 'Błąd przy weryfikacji kodu');
        setLoading(false);
        return;
      }

      if (!result.data) {
        setError('Nieznany błąd');
        setLoading(false);
        return;
      }

      // Zapisz backup codes z response
      if (result.data.backupCodes && Array.isArray(result.data.backupCodes)) {
        setBackupCodes(result.data.backupCodes);
      }

      // Pokaż backup codes modal
      setShowBackupCodesModal(true);
    } catch (err) {
      setError('Nieznany błąd - sprawdź konsolę');
      console.error('2FA enable error:', err);
      setLoading(false);
    }
  };

  const handleBackupCodesConfirm = () => {
    // Przejdź do logowania - sesja wyczyści się automatycznie
    navigate('/admin/login', { replace: true });
  };

  return (
    <div className="auth-page">
      <div className="auth-card admin-card setup-2fa-card">
        <div className="admin-badge">🔐 SETUP 2FA</div>
        <h1>Konfiguracja Dwustopniowej Weryfikacji</h1>
        <p className="admin-subtitle">Skanuj kod QR aby włączyć 2FA</p>

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
          <>
            <BackupCodesModal
              backupCodes={backupCodes}
              onConfirm={handleBackupCodesConfirm}
              isOpen={true}
            />
          </>
        )}
      </div>
    </div>
  );
};
