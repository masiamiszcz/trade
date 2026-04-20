/**
 * User Authentication Types
 * 2FA Registration Flow: 2-step mandatory verification
 * 2FA Login Flow: Optional (if user enabled 2FA)
 */

// ============ REGISTRATION REQUESTS ============

/**
 * User Registration Step 1: Provide username, email, password
 * Returns: temp token + QR code for 2FA setup
 */
export interface UserRegisterInitialRequest {
  username: string;
  email: string;
  firstName: string;
  lastName: string;
  password: string;
  baseCurrency: string;
}

/**
 * User Registration Step 2: Verify 2FA code
 * Returns: final token, user created in database
 */
export interface UserRegisterComplete2FARequest {
  sessionId: string;
  code: string;
}

// ============ LOGIN REQUESTS ============

/**
 * User Login Step 1: Provide username/email and password
 * Returns: 
 *  - If 2FA disabled: final token (user logged in)
 *  - If 2FA enabled: temp token (requires step 2)
 */
export interface UserLoginInitialRequest {
  userNameOrEmail: string;
  password: string;
}

/**
 * User Login Step 2: Verify 2FA code (only if 2FA enabled)
 * Returns: final token
 */
export interface UserVerifyLogin2FARequest {
  sessionId: string;
  code: string;
}

// ============ 2FA MANAGEMENT REQUESTS ============

/**
 * User initiates 2FA setup
 * Returns: QR code + manual key + backup codes
 */
export interface UserSetup2FAInitialRequest {
  // Empty - uses authenticated user context
}

/**
 * User confirms 2FA setup
 * Returns: success + backup codes confirmation
 */
export interface UserSetup2FAEnableRequest {
  sessionId: string;
  code: string;
}

/**
 * User disables 2FA
 */
export interface UserDisable2FARequest {
  code: string;
}

// ============ REGISTRATION RESPONSES ============

/**
 * Response from UserRegisterInitialRequest
 * Step 1: User provides registration data
 */
export interface UserRegistrationInitialResponse {
  token: string; // Temp token (5 min)
  sessionId: string;
  qrCodeDataUrl: string;
  manualKey: string; // Base32 encoded secret
  backupCodes: string[];
  message: string;
}

/**
 * Response from UserRegisterComplete2FARequest
 * Step 2: User verified 2FA code, user created in database
 */
export interface UserRegistrationCompleteResponse {
  token: string; // Final token (60 min)
  userId: string;
  username: string;
  email: string;
  expiresAt: number;
  message: string;
  backupCodes?: string[]; // Backup codes for account recovery
}

// ============ LOGIN RESPONSES ============

/**
 * Response from UserLoginInitialRequest
 * Step 1: Password verification
 */
export interface UserLoginInitialResponse {
  token: string; // 5 min if 2FA required, 60 min if 2FA disabled
  sessionId: string;
  requiresTwoFactor: boolean;
  username: string;
}

/**
 * Response from UserVerifyLogin2FARequest
 * Step 2: 2FA code verification
 */
export interface UserAuthCompleteResponse {
  token: string; // Final token (60 min)
  userId: string;
  username: string;
  expiresAt: number;
  role: string;
}

// ============ 2FA MANAGEMENT RESPONSES ============

/**
 * Response from UserSetup2FAInitialRequest
 */
export interface UserTwoFactorSetupResponse {
  qrCodeDataUrl: string;
  manualKey: string;
  sessionId: string;
  backupCodes: string[];
  message: string;
}

/**
 * Response from UserSetup2FAEnableRequest
 */
export interface UserTwoFactorEnableResponse {
  backupCodes: string[];
  success: boolean;
  message: string;
}

/**
 * Response from UserDisable2FARequest
 */
export interface UserTwoFactorDisableResponse {
  success: boolean;
  message: string;
}

/**
 * 2FA Status for current user
 */
export interface UserTwoFactorStatusResponse {
  twoFactorEnabled: boolean;
  remainingBackupCodes?: number;
}
