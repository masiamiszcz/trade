import React from 'react';
import React, { useEffect, useRef } from 'react';
import QRCode from 'qrcode';

interface QRCodeDisplayProps {
  qrCodeDataUrl?: string;
  manualKey: string;
  showManualKey: boolean;
  onToggleManualKey: () => void;
}

export const QRCodeDisplay: React.FC<QRCodeDisplayProps> = ({
  manualKey,
  showManualKey,
  onToggleManualKey,
}) => {
  const canvasRef = useRef<HTMLCanvasElement>(null);

  useEffect(() => {
    if (canvasRef.current && manualKey) {
      const otpauthUrl = `otpauth://totp/Trading%20Platform?secret=${manualKey}&issuer=Trading%20Platform`;
      QRCode.toCanvas(canvasRef.current, otpauthUrl, {
        width: 256,
        margin: 1,
        color: {
          dark: '#000000',
          light: '#FFFFFF',
        },
      }).catch(err => console.error('Failed to generate QR code:', err));
    }
  }, [manualKey]);

  return (
    <div className="qr-code-container">
      <div className="qr-code-section">
        <h3>Skanuj kod QR</h3>
        <p>Użyj Google Authenticator, Microsoft Authenticator lub Authy</p>
        
        <div className="qr-code-display">
          <canvas ref={canvasRef} className="qr-code-image" />
        </div>

        <button
          type="button"
          className="btn-secondary"
          onClick={onToggleManualKey}
        >
          {showManualKey ? 'Ukryj kod ręczny' : 'Nie mogę skanować? Wpisz ręcznie'}
        </button>

        {showManualKey && (
          <div className="manual-key-section">
            <p className="manual-key-label">Kod do wpisania ręcznie:</p>
            <code className="manual-key-code">{manualKey}</code>
            <button
              type="button"
              className="btn-copy"
              onClick={() => {
                navigator.clipboard.writeText(manualKey);
                alert('Kod skopiowany!');
              }}
            >
              Skopiuj
            </button>
          </div>
        )}
      </div>
    </div>
  );
};
