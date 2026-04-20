// Admin Auth Types
// Admin Authentication Types

// Bootstrap Request
export interface AdminBootstrapRequest {
  username: string;
  email: string;
  password: string;
}

// Login Request
export interface AdminLoginRequest {
  usernameOrEmail: string;
  password: string;
}

// Verify 2FA Request
export interface AdminVerify2FARequest {
  sessionId: string;
  code: string;
}

// Setup 2FA Request
export interface AdminSetup2FARequest {
  code: string;
}

// Register via Invitation Request
export interface AdminRegisterViaInviteRequest {
  token: string;
  username: string;
  password: string;
}

// Disable 2FA Request
export interface AdminDisable2FARequest {
  code: string;
}

// Regenerate Backup Codes Request
export interface AdminRegenerateBackupCodesRequest {
  code: string;
}

// Invite Admin Request
export interface AdminInviteRequest {
  email: string;
  firstName: string;
  lastName: string;
  permissions?: string[];
}

// Admin Registration Response
export interface AdminRegistrationResponse {
  token: string;
  sessionId: string;
  requiresTwoFactorSetup: boolean;
  message: string;
  qrCodeDataUrl?: string;
  manualKey?: string;
  backupCodes?: string[];
}

// Admin Login Response (Step 1)
export interface AdminLoginResponse {
  token: string;
  sessionId: string;
  requiresTwoFactor: boolean;
  username: string;
}

// Admin Auth Success Response (Step 2 - Final JWT)
export interface AdminAuthSuccessResponse {
  token: string;
  role: string;
  adminId: string;
  username: string;
  expiresAt: number;
}

// 2FA Setup Response
export interface AdminTwoFactorSetupResponse {
  qrCodeDataUrl: string;
  manualKey: string;
  sessionId: string;
  message: string;
}

// 2FA Complete Response
export interface AdminTwoFactorCompleteResponse {
  backupCodes: string[];
  success: boolean;
  message: string;
}

// 2FA Disable Response
export interface AdminTwoFactorDisableResponse {
  success: boolean;
  message: string;
}

// Admin Invitation Response
export interface AdminInvitationResponse {
  token: string;
  email: string;
  expiresAt: string;
  invitationUrl: string;
}

// Admin Auth Error Response
export interface AdminAuthErrorResponse {
  statusCode: number;
  message: string;
  errorCode: string;
}

// Admin Session State (stored in localStorage + context)
export interface AdminSessionState {
  token: string | null;
  sessionId: string | null;
  adminId: string | null;
  username: string | null;
  isTempToken: boolean;
  requiresTwoFactor: boolean;
}
