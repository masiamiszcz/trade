
import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { adminAuthService } from '../services/AdminAuthService';
import { useAdminAuth } from '../hooks/admin/useAdminAuth';
import { QRCodeDisplay } from '../components/admin/2FA/QRCodeDisplay';
import { BackupCodesModal } from '../components/admin/2FA/BackupCodesModal';
import { TwoFactorInput } from '../components/admin/2FA/TwoFactorInput';
import { AdminSetup2FARequest } from '../types/adminAuth';

export const AdminSetup2FAPage: React.FC = () => {
  const navigate = useNavigate();
  const { token } = useAdminAuth();

  const [showManualKey, setShowManualKey] = useState(false);
  const [showBackupCodesModal, setShowBackupCodesModal] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string>('');
  const [backupCodes, setBackupCodes] = useState<string[]>([]);
  const [manualKey, setManualKey] = useState<string>('');
  const [qrCodeDataUrl, setQrCodeDataUrl] = useState<string>('');
  const [setupSessionId, setSetupSessionId] = useState<string>('');

  // Redirect if no temp token
  useEffect(() => {
    if (!token) {
      navigate('/admin/register', { replace: true });
      return;
    }

    // Generate TOTP secret on mount
    const generateSecret = async () => {
      try {
        const result = await adminAuthService.adminGenerateSetup2FA(token);
        
        if (result.error) {
          setError(result.error.message || 'Błąd przy generowaniu 2FA');
          setLoading(false);
          return;
        }

        if (!result.data) {
          setError('Nieznany błąd - brak danych');
          setLoading(false);
          return;
        }

        setManualKey(result.data.manualKey);
        setQrCodeDataUrl(result.data.qrCodeDataUrl);
        setSetupSessionId(result.data.sessionId);
        setLoading(false);
      } catch (err) {
        setError('Błąd przy generowaniu kodu QR');
        console.error('Generate 2FA error:', err);
        setLoading(false);
      }
    };

    generateSecret();
  }, [token, navigate]);

  const handleCodeSubmit = async (code: string) => {
    setError('');
    setLoading(true);

    try {
      const request: AdminSetup2FARequest = { code, sessionId: setupSessionId };
      const result = await adminAuthService.adminEnableSetup2FA(request, token!);

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

        {loading && <div className="loading-message">Generuję kod QR...</div>}
        {error && <div className="error-message">{error}</div>}

        {!loading && !showBackupCodesModal ? (
          <>
            {manualKey && qrCodeDataUrl ? (
              <>
                <QRCodeDisplay
                  manualKey={manualKey}
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
              <div className="error-message">Nie udało się wygenerować kodu QR</div>
            )}
          </>
        ) : null}

        {showBackupCodesModal ? (
          <>
            <BackupCodesModal
              backupCodes={backupCodes}
              onConfirm={handleBackupCodesConfirm}
              isOpen={true}
            />
          </>
        ) : null}
      </div>
    </div>
  );
};
