/**
 * Unified Authentication Service
 * Handles all user and admin authentication operations
 * 
 * Uses centralized HTTP client with interceptors,
 * proper error handling, and request logging
 * 
 * Supports:
 * - User login/register
 * - Admin bootstrap/login/2FA
 * - Token management
 */

import { httpClient } from './http/HttpClient';
import { ApiError, isApiError, getErrorMessage } from './http/ApiError';
import { API_CONFIG } from '../config/apiConfig';
import type { LoginRequest, RegisterRequest, AuthResponse } from '../types';
import type {
  AdminBootstrapRequest,
  AdminLoginRequest,
  AdminVerify2FARequest,
  AdminSetup2FARequest,
  AdminRegisterViaInviteRequest,
  AdminInviteRequest,
} from '../types/adminAuth';
import type {
  UserRegisterInitialRequest,
  UserRegisterComplete2FARequest,
  UserLoginInitialRequest,
  UserVerifyLogin2FARequest,
  UserRegistrationInitialResponse,
  UserRegistrationCompleteResponse,
  UserLoginInitialResponse,
  UserAuthCompleteResponse,
  UserSetup2FAInitialRequest,
  UserSetup2FAEnableRequest,
  UserDisable2FARequest,
  UserTwoFactorSetupResponse,
  UserTwoFactorEnableResponse,
  UserTwoFactorDisableResponse,
  UserTwoFactorStatusResponse,
} from '../types/userAuth';

/**
 * Token storage keys
 * IMPORTANT: Must match useAuth.tsx and AdminAuthContext.tsx keys!
 */
const USER_TOKEN_STORAGE_KEY = 'auth_token';              // FINAL user token
const USER_TEMP_TOKEN_STORAGE_KEY = 'trading-platform-temp-token';  // TEMP user token (2FA)
const USER_SESSION_ID_KEY = 'trading-platform-session-id';          // Session ID for 2FA

// Admin uses unified session storage (see AdminAuthContext.tsx)
const ADMIN_SESSION_STORAGE_KEY = 'trading-admin-session';

/**
 * Unified Authentication Service
 * 
 * TOKEN MANAGEMENT RULES:
 * - FINAL tokens: stored in specific keys (auth_token for user, trading-admin-session for admin)
 * - TEMP tokens: stored separately to never override final tokens
 * - 2FA FLOW: temp token used ONLY for /verify-2fa endpoints, then replaced by final token
 */
export class AuthenticationService {
  /**
   * Get stored final USER token
   */
  getUserToken(): string | null {
    return localStorage.getItem(USER_TOKEN_STORAGE_KEY);
  }

  /**
   * Get stored TEMPORARY user token (for 2FA)
   * DO NOT use this for general API calls!
   */
  private getUserTempToken(): string | null {
    return localStorage.getItem(USER_TEMP_TOKEN_STORAGE_KEY);
  }

  /**
   * Get stored ADMIN session (contains final or temp token)
   */
  getAdminSession(): any {
    try {
      const stored = localStorage.getItem(ADMIN_SESSION_STORAGE_KEY);
      return stored ? JSON.parse(stored) : null;
    } catch {
      return null;
    }
  }

  /**
   * Store USER token (FINAL)
   * This is the main authentication token
   */
  setUserToken(token: string): void {
    localStorage.setItem(USER_TOKEN_STORAGE_KEY, token);
    // Clear temp token when final token is set
    localStorage.removeItem(USER_TEMP_TOKEN_STORAGE_KEY);
    localStorage.removeItem(USER_SESSION_ID_KEY);
    // Emit custom event so useAuth hook updates in same tab
    window.dispatchEvent(new CustomEvent('userTokenUpdated', { detail: { token } }));
  }

  /**
   * Store USER TEMP token (for 2FA flow ONLY)
   * 
   * ⚠️ CRITICAL: This must NOT override the final token!
   * It's stored separately and cleared once 2FA verification completes.
   * 
   * @param tempToken - Temporary token from login step 1
   * @param sessionId - Session ID for 2FA verification
   */
  private setUserTempToken(tempToken: string, sessionId: string): void {
    localStorage.setItem(USER_TEMP_TOKEN_STORAGE_KEY, tempToken);
    localStorage.setItem(USER_SESSION_ID_KEY, sessionId);
    console.log('[AuthService] 🔐 TEMP token stored (2FA mode)');
  }

  /**
   * Store ADMIN session
   */
  setAdminSession(sessionData: any): void {
    localStorage.setItem(ADMIN_SESSION_STORAGE_KEY, JSON.stringify(sessionData));
    window.dispatchEvent(new CustomEvent('adminSessionUpdated', { detail: { session: sessionData } }));
  }

  /**
   * Clear all USER tokens
   */
  clearUserTokens(): void {
    localStorage.removeItem(USER_TOKEN_STORAGE_KEY);
    localStorage.removeItem(USER_TEMP_TOKEN_STORAGE_KEY);
    localStorage.removeItem(USER_SESSION_ID_KEY);
    window.dispatchEvent(new CustomEvent('userTokenUpdated', { detail: { token: null } }));
  }

  /**
   * Clear all ADMIN tokens
   */
  clearAdminSession(): void {
    localStorage.removeItem(ADMIN_SESSION_STORAGE_KEY);
    window.dispatchEvent(new CustomEvent('adminSessionUpdated', { detail: { session: null } }));
  }

  /**
   * Clear all tokens
   */
  clearAllTokens(): void {
    this.clearUserTokens();
    this.clearAdminSession();
  }

  /**
   * User login
   */
  async userLogin(request: LoginRequest): Promise<AuthResponse> {
    try {
      const response = await httpClient.fetch<AuthResponse>({
        url: API_CONFIG.endpoints.auth.login,
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(request),
      });

      if (response.token) {
        this.setUserToken(response.token);
      }

      return response;
    } catch (error) {
      throw this.handleError(error);
    }
  }

  /**
   * User register
   */
  async userRegister(request: RegisterRequest): Promise<AuthResponse> {
    try {
      const response = await httpClient.fetch<AuthResponse>({
        url: API_CONFIG.endpoints.auth.register,
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(request),
      });

      if (response.token) {
        this.setUserToken(response.token);
      }

      return response;
    } catch (error) {
      throw this.handleError(error);
    }
  }

  /**
   * User logout
   */
  async userLogout(): Promise<void> {
    try {
      const token = this.getUserToken();
      if (token) {
        await httpClient.fetch<void>({
          url: API_CONFIG.endpoints.auth.logout,
          method: 'POST',
        });
      }
    } finally {
      this.clearUserTokens();
    }
  }

  /**
   * Admin bootstrap (initial setup)
   * Returns temporary token for 2FA
   */
  async adminBootstrap(
    request: AdminBootstrapRequest
  ): Promise<{ token: string; invitationCode: string }> {
    try {
      const response = await httpClient.fetch<{ token: string; invitationCode: string }>({
        url: API_CONFIG.endpoints.admin.bootstrap,
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(request),
      });

      // Bootstrap returns TEMP token for 2FA
      if (response.token) {
        this.setAdminSession({
          token: response.token,
          isTempToken: true,
          requiresTwoFactor: true,
        });
      }

      return response;
    } catch (error) {
      throw this.handleError(error);
    }
  }

  /**
   * Admin login
   * Step 1: Returns temporary token if 2FA required
   */
  async adminLogin(
    request: AdminLoginRequest
  ): Promise<{ token?: string; requiresTwoFactor?: boolean; sessionId?: string }> {
    try {
      const response = await httpClient.fetch<{
        token?: string;
        requiresTwoFactor?: boolean;
        sessionId?: string;
      }>({
        url: API_CONFIG.endpoints.admin.login,
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(request),
      });

      if (response.token) {
        // Check if 2FA is required
        if (response.requiresTwoFactor) {
          // Store TEMP token for 2FA verification
          this.setAdminSession({
            token: response.token,
            sessionId: response.sessionId,
            isTempToken: true,
            requiresTwoFactor: true,
          });
          console.log('[AuthService] 🔐 Admin TEMP token stored (2FA required)');
        } else {
          // No 2FA - store as final token
          this.setAdminSession({
            token: response.token,
            isTempToken: false,
            requiresTwoFactor: false,
          });
        }
      }

      return response;
    } catch (error) {
      throw this.handleError(error);
    }
  }

  /**
   * Admin verify 2FA
   * Step 2: Exchanges temp token for final token
   */
  async adminVerify2FA(
    request: AdminVerify2FARequest
  ): Promise<{ token: string }> {
    try {
      const response = await httpClient.fetch<{ token: string }>({
        url: API_CONFIG.endpoints.admin.verify2fa,
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(request),
      });

      if (response.token) {
        // Store FINAL token, mark as not temp
        const currentSession = this.getAdminSession();
        this.setAdminSession({
          ...currentSession,
          token: response.token,
          isTempToken: false,
          requiresTwoFactor: false,
        });
        console.log('[AuthService] ✅ Admin FINAL token stored (2FA verified)');
      }

      return response;
    } catch (error) {
      throw this.handleError(error);
    }
  }

  /**
   * Admin setup 2FA - Generate QR code
   * GET /auth/admin/setup-2fa/generate
   * Returns: { qrCodeDataUrl, manualKey, sessionId, message }
   * 
   * NOTE: Temp token is automatically injected by HttpClient
   */
  async adminGenerateSetup2FA(): Promise<any> {
    try {
      return await httpClient.fetch<any>({
        url: API_CONFIG.endpoints.admin.setup2faGenerate,
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
        },
      });
    } catch (error) {
      throw this.handleError(error);
    }
  }

  /**
   * Admin enable 2FA - Confirm QR scan with code
   * POST /auth/admin/setup-2fa/enable
   * Returns: { backupCodes, success, message }
   * 
   * NOTE: Temp token is automatically injected by HttpClient
   */
  async adminEnableSetup2FA(request: AdminSetup2FARequest): Promise<any> {
    try {
      return await httpClient.fetch<any>({
        url: API_CONFIG.endpoints.admin.setup2faEnable,
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(request),
      });
    } catch (error) {
      throw this.handleError(error);
    }
  }

  /**
   * Admin disable 2FA
   * POST /auth/admin/setup-2fa/disable
   */
  async adminDisable2FA(request: any, token: string): Promise<any> {
    try {
      return await httpClient.fetch<any>({
        url: API_CONFIG.endpoints.admin.setup2faDisable,
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(request),
      });
    } catch (error) {
      throw this.handleError(error);
    }
  }

  /**
   * Admin regenerate backup codes
   * POST /auth/admin/backup-codes/regenerate
   */
  async adminRegenerateBackupCodes(request: any, token: string): Promise<any> {
    try {
      return await httpClient.fetch<any>({
        url: API_CONFIG.endpoints.admin.backupCodesRegenerate,
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          ...(token && { 'Authorization': `Bearer ${token}` }),
        },
        body: JSON.stringify(request),
      });
    } catch (error) {
      throw this.handleError(error);
    }
  }

  /**
   * Admin register via invitation
   */
  async adminRegisterViaInvite(
    request: AdminRegisterViaInviteRequest
  ): Promise<{ token: string }> {
    try {
      const response = await httpClient.fetch<{ token: string }>({
        url: API_CONFIG.endpoints.admin.register,
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(request),
      });

      if (response.token) {
        // Store as FINAL token
        this.setAdminSession({
          token: response.token,
          isTempToken: false,
        });
      }

      return response;
    } catch (error) {
      throw this.handleError(error);
    }
  }

  /**
   * Admin send invitation
   */
  async adminSendInvitation(
    request: AdminInviteRequest
  ): Promise<{ invitationCode: string }> {
    try {
      // Admin session is managed automatically by httpClient
      // No need to manually add Authorization header
      return await httpClient.fetch<{ invitationCode: string }>({
        url: API_CONFIG.endpoints.admin.invite,
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(request),
      });
    } catch (error) {
      throw this.handleError(error);
    }
  }

  /**
   * Admin invite (alias for adminSendInvitation)
   * Used by old AdminAuthService interface
   */
  async adminInvite(request: AdminInviteRequest, token: string): Promise<any> {
    try {
      return await httpClient.fetch<any>({
        url: API_CONFIG.endpoints.admin.invite,
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(request),
      });
    } catch (error) {
      throw this.handleError(error);
    }
  }

  // ============ USER 2FA METHODS ============

  /**
   * User Registration Step 1: Provide registration data, get temp token + QR code for 2FA
   * 
   * Returns: temp token (valid for ~5 minutes) + QR code
   */
  async userRegisterInitial(request: UserRegisterInitialRequest): Promise<UserRegistrationInitialResponse> {
    try {
      const response = await httpClient.fetch<UserRegistrationInitialResponse>({
        url: '/user/register',
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(request),
      });

      // If response includes temp token, store it separately
      // DO NOT store as main token!
      if (response.token && response.sessionId) {
        this.setUserTempToken(response.token, response.sessionId);
      }

      return response;
    } catch (error) {
      throw this.handleError(error);
    }
  }

  /**
   * User Registration Step 2: Verify 2FA code, create account, get FINAL token
   * 
   * ⚠️ CRITICAL FLOW:
   * Step 1: userRegisterInitial() returns TEMP token (stored separately)
   * Step 2: userRegisterComplete2FA() exchanges TEMP token for FINAL token
   * 
   * @param request - Contains sessionId and 2FA code
   * 
   * NOTE: Temp token is automatically injected by HttpClient
   */
  async userRegisterComplete2FA(request: UserRegisterComplete2FARequest): Promise<UserRegistrationCompleteResponse> {
    try {
      // httpClient will automatically use tempToken because endpoint is /register/verify-2fa
      const response = await httpClient.fetch<UserRegistrationCompleteResponse>({
        url: '/user/register/verify-2fa',
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ sessionId: request.sessionId, code: request.code }),
      });

      if (response.token) {
        // This is the FINAL token after 2FA verification
        this.setUserToken(response.token);
        console.log('[AuthService] ✅ User FINAL token stored (registration 2FA verified)');
      }

      return response;
    } catch (error) {
      throw this.handleError(error);
    }
  }

  /**
   * User Login Step 1: Verify password, return temp token if 2FA required
   * 
   * Returns:
   * - If 2FA not required: final token (set immediately)
   * - If 2FA required: temp token + sessionId (for Step 2)
   */
  async userLoginInitial(request: UserLoginInitialRequest): Promise<UserLoginInitialResponse> {
    try {
      const response = await httpClient.fetch<UserLoginInitialResponse>({
        url: '/user/login',
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(request),
      });

      if (response.token) {
        // Check if 2FA is required
        if (response.requiresTwoFactor && response.sessionId) {
          // Store TEMP token for Step 2
          this.setUserTempToken(response.token, response.sessionId);
          console.log('[AuthService] 🔐 User TEMP token stored (login 2FA required)');
        } else {
          // No 2FA required - store FINAL token immediately
          this.setUserToken(response.token);
          console.log('[AuthService] ✅ User FINAL token stored (no 2FA)');
        }
      }

      return response;
    } catch (error) {
      throw this.handleError(error);
    }
  }

  /**
   * User Login Step 2: Verify 2FA code, get final token
   * 
   * ⚠️ CRITICAL FLOW:
   * Step 1: userLoginInitial() returns TEMP token if 2FA required
   * Step 2: userVerifyLogin2FA() exchanges TEMP token for FINAL token
   * 
   * @param request - Contains sessionId and 2FA code
   * @param tempToken - Temporary token from Step 1 (passed explicitly to ensure clarity)
   */
  async userVerifyLogin2FA(request: UserVerifyLogin2FARequest, tempToken: string): Promise<UserAuthCompleteResponse> {
    try {
      console.log('[AuthService] 🔐 Verifying login 2FA:', {
        sessionId: request.sessionId,
        code: request.code,
        tempTokenExists: !!tempToken,
        tempTokenLength: tempToken?.length || 0,
      });

      // httpClient will automatically use tempToken because endpoint is /verify-2fa
      const response = await httpClient.fetch<UserAuthCompleteResponse>({
        url: '/user/verify-2fa',
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ sessionId: request.sessionId, code: request.code }),
      });

      if (response.token) {
        // This is the FINAL token after 2FA verification
        this.setUserToken(response.token);
        console.log('[AuthService] ✅ User FINAL token stored (login 2FA verified)');
      }

      return response;
    } catch (error) {
      console.error('[AuthService] ❌ Login 2FA verification failed:', error);
      throw this.handleError(error);
    }
  }

  /**
   * User-initiated 2FA setup (optional, after login)
   * 
   * NOTE: HttpClient automatically injects appropriate token
   * (final token for authenticated users)
   */
  async userSetup2FAInitial(request: UserSetup2FAInitialRequest): Promise<UserTwoFactorSetupResponse> {
    try {
      return await httpClient.fetch<UserTwoFactorSetupResponse>({
        url: '/user/2fa-setup',
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(request),
      });
    } catch (error) {
      throw this.handleError(error);
    }
  }

  /**
   * User confirms 2FA setup
   * 
   * NOTE: HttpClient automatically injects appropriate token
   * (final token for authenticated users)
   */
  async userSetup2FAEnable(request: UserSetup2FAEnableRequest): Promise<UserTwoFactorEnableResponse> {
    try {
      return await httpClient.fetch<UserTwoFactorEnableResponse>({
        url: '/user/2fa-enable',
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ sessionId: request.sessionId, code: request.code }),
      });
    } catch (error) {
      throw this.handleError(error);
    }
  }

  /**
   * User disables 2FA
   */
  async userDisable2FA(request: UserDisable2FARequest): Promise<UserTwoFactorDisableResponse> {
    try {
      // httpClient will automatically inject final token
      return await httpClient.fetch<UserTwoFactorDisableResponse>({
        url: '/user/2fa-disable',
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(request),
      });
    } catch (error) {
      throw this.handleError(error);
    }
  }

  /**
   * Check user's 2FA status
   */
  async userGet2FAStatus(): Promise<UserTwoFactorStatusResponse> {
    try {
      const token = this.getUserToken();
      return await httpClient.fetch<UserTwoFactorStatusResponse>({
        url: '/user/2fa-status',
        method: 'GET',
      });
    } catch (error) {
      throw this.handleError(error);
    }
  }

  /**
   * Handle and normalize errors
   */
  private handleError(error: unknown): Error {
    if (isApiError(error)) {
      // Log error details for debugging
      console.error('API Error:', error.getDetails());

      // Return ApiError as-is for proper handling in components
      return error;
    }

    if (error instanceof Error) {
      return error;
    }

    return new Error(getErrorMessage(error));
  }

  /**
   * Check if user is authenticated
   */
  isUserAuthenticated(): boolean {
    return !!this.getUserToken();
  }

  /**
   * Check if admin is authenticated
   */
  isAdminAuthenticated(): boolean {
    return !!this.getAdminSession()?.token;
  }
}

// Export singleton instance
export const authService = new AuthenticationService();

// Initialize response interceptor to handle 401 (unauthorized)
httpClient.addResponseInterceptor((response) => {
  if (response.status === 401) {
    // Clear tokens on unauthorized
    authService.clearAllTokens();
    
    // Optionally trigger logout event or redirect
    window.dispatchEvent(new CustomEvent('auth:unauthorized'));
  }

  return response;
});

// Log initialization in development
if (process.env.NODE_ENV === 'development') {
  console.log('%c✓ Authentication Service initialized (httpClient handles all token injection)', 'color: #00aa00; font-weight: bold');
}

export default authService;
