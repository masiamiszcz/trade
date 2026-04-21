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
 * Token storage key
 */
const TOKEN_STORAGE_KEY = 'auth_token';
const ADMIN_TOKEN_STORAGE_KEY = 'admin_auth_token';

/**
 * Unified Authentication Service
 */
class AuthenticationService {
  /**
   * Get stored user token
   */
  getUserToken(): string | null {
    return localStorage.getItem(TOKEN_STORAGE_KEY);
  }

  /**
   * Get stored admin token
   */
  getAdminToken(): string | null {
    return localStorage.getItem(ADMIN_TOKEN_STORAGE_KEY);
  }

  /**
   * Store user token and emit event for useAuth hook to pick up
   */
  setUserToken(token: string): void {
    localStorage.setItem(TOKEN_STORAGE_KEY, token);
    // Emit custom event so useAuth hook updates in same tab (storage events don't fire in same tab!)
    window.dispatchEvent(new CustomEvent('userTokenUpdated', { detail: { token } }));
  }

  /**
   * Store admin token and emit event
   */
  setAdminToken(token: string): void {
    localStorage.setItem(ADMIN_TOKEN_STORAGE_KEY, token);
    window.dispatchEvent(new CustomEvent('adminTokenUpdated', { detail: { token } }));
  }

  /**
   * Clear user token and emit event
   */
  clearUserToken(): void {
    localStorage.removeItem(TOKEN_STORAGE_KEY);
    window.dispatchEvent(new CustomEvent('userTokenUpdated', { detail: { token: null } }));
  }

  /**
   * Clear admin token and emit event
   */
  clearAdminToken(): void {
    localStorage.removeItem(ADMIN_TOKEN_STORAGE_KEY);
    window.dispatchEvent(new CustomEvent('adminTokenUpdated', { detail: { token: null } }));
  }

  /**
   * Clear all tokens
   */
  clearAllTokens(): void {
    this.clearUserToken();
    this.clearAdminToken();
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
          headers: {
            'Authorization': `Bearer ${token}`,
          },
        });
      }
    } finally {
      this.clearUserToken();
    }
  }

  /**
   * Admin bootstrap (initial setup)
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

      if (response.token) {
        this.setAdminToken(response.token);
      }

      return response;
    } catch (error) {
      throw this.handleError(error);
    }
  }

  /**
   * Admin login
   */
  async adminLogin(
    request: AdminLoginRequest
  ): Promise<{ token?: string; requiresTwoFactor?: boolean }> {
    try {
      const response = await httpClient.fetch<{
        token?: string;
        requiresTwoFactor?: boolean;
      }>({
        url: API_CONFIG.endpoints.admin.login,
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(request),
      });

      if (response.token) {
        this.setAdminToken(response.token);
      }

      return response;
    } catch (error) {
      throw this.handleError(error);
    }
  }

  /**
   * Admin verify 2FA
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
        this.setAdminToken(response.token);
      }

      return response;
    } catch (error) {
      throw this.handleError(error);
    }
  }

  /**
   * Admin setup 2FA
   */
  async adminSetup2FA(
    request: AdminSetup2FARequest
  ): Promise<{ qrCode: string; manualKey: string }> {
    try {
      const token = this.getAdminToken();
      return await httpClient.fetch<{ qrCode: string; manualKey: string }>({
        url: API_CONFIG.endpoints.admin.setup2fa,
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
        url: API_CONFIG.endpoints.admin.invitations,
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(request),
      });

      if (response.token) {
        this.setAdminToken(response.token);
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
      const token = this.getAdminToken();
      return await httpClient.fetch<{ invitationCode: string }>({
        url: API_CONFIG.endpoints.admin.invitations,
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

  // ============ USER 2FA METHODS ============

  /**
   * User Registration Step 1: Provide registration data, get QR code for 2FA
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

      return response;
    } catch (error) {
      throw this.handleError(error);
    }
  }

  /**
   * User Registration Step 2: Verify 2FA code, create user account
   */
  async userRegisterComplete2FA(request: UserRegisterComplete2FARequest, tempToken: string): Promise<UserRegistrationCompleteResponse> {
    try {
      const response = await httpClient.fetch<UserRegistrationCompleteResponse>({
        url: '/user/register/verify-2fa',
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${tempToken}`,
        },
        body: JSON.stringify({ sessionId: request.sessionId, code: request.code }),
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
   * User Login Step 1: Verify password, return temp token if 2FA required
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

      // If 2FA not required, set token immediately
      if (!response.requiresTwoFactor && response.token) {
        this.setUserToken(response.token);
      }

      return response;
    } catch (error) {
      throw this.handleError(error);
    }
  }

  /**
   * User Login Step 2: Verify 2FA code, get final token
   * Requires temp token from Step 1 to be passed in Authorization header
   */
  async userVerifyLogin2FA(request: UserVerifyLogin2FARequest, tempToken: string): Promise<UserAuthCompleteResponse> {
    try {
      console.log('[userVerifyLogin2FA] Starting 2FA verification with:', {
        sessionId: request.sessionId,
        code: request.code,
        tempTokenExists: !!tempToken,
        tempTokenLength: tempToken?.length || 0,
        tempTokenPreview: tempToken ? tempToken.substring(0, 20) + '...' : 'UNDEFINED',
      });





      const response = await httpClient.fetch<UserAuthCompleteResponse>({
        url: '/user/verify-2fa',
        method: 'POST',
        headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${tempToken}`,
        },
        body: JSON.stringify({ sessionId: request.sessionId, code: request.code }),
      });

      console.log('[userVerifyLogin2FA] Response received:', { success: !!response.token });

      if (response.token) {
        this.setUserToken(response.token);
      }

      return response;
    } catch (error) {
      console.error('[userVerifyLogin2FA] Error:', error);
      throw this.handleError(error);
    }
  }

  /**
   * User-initiated 2FA setup (optional, after login)
   */
  async userSetup2FAInitial(request: UserSetup2FAInitialRequest): Promise<UserTwoFactorSetupResponse> {
    try {
      const token = this.getUserToken();
      return await httpClient.fetch<UserTwoFactorSetupResponse>({
        url: '/user/2fa-setup',
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
   * User confirms 2FA setup
   */
  async userSetup2FAEnable(request: UserSetup2FAEnableRequest): Promise<UserTwoFactorEnableResponse> {
    try {
      const response = await httpClient.fetch<UserTwoFactorEnableResponse>({
        url: '/user/2fa-enable',
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ sessionId: request.sessionId, code: request.code }),
      });

      return response;
    } catch (error) {
      throw this.handleError(error);
    }
  }

  /**
   * User disables 2FA
   */
  async userDisable2FA(request: UserDisable2FARequest): Promise<UserTwoFactorDisableResponse> {
    try {
      const token = this.getUserToken();
      return await httpClient.fetch<UserTwoFactorDisableResponse>({
        url: '/user/2fa-disable',
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
   * Check user's 2FA status
   */
  async userGet2FAStatus(): Promise<UserTwoFactorStatusResponse> {
    try {
      const token = this.getUserToken();
      return await httpClient.fetch<UserTwoFactorStatusResponse>({
        url: '/user/2fa-status',
        method: 'GET',
        headers: {
          ...(token && { 'Authorization': `Bearer ${token}` }),
        },
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
    return !!this.getAdminToken();
  }
}

// Export singleton instance
export const authService = new AuthenticationService();

// Initialize request interceptor to add auth tokens
httpClient.addRequestInterceptor((config) => {
  // Don't override Authorization header if already provided (e.g., tempToken for 2FA)
  if (config.headers && (config.headers as Record<string, string>)['Authorization']) {
    console.log('[Interceptor] Authorization header already set, skipping token injection for:', config.url);
    return config;
  }

  // Public endpoints that should NOT have auto-injected tokens
  // These endpoints either don't require auth or handle auth explicitly (tempToken)
  const publicEndpoints = [
    '/user/register',           // Registration - no auth needed
    '/user/register/verify-2fa', // 2FA verification - uses tempToken explicitly
    '/user/login',              // Login - no auth needed
    '/user/verify-2fa',         // Login 2FA verification - no auth needed
  ];

  // Check if this is a public endpoint
  const isPublicEndpoint = publicEndpoints.some(endpoint => config.url?.includes(endpoint));
  if (isPublicEndpoint) {
    console.log('[Interceptor] Public endpoint detected, skipping token injection:', config.url);
    return config;
  }

  const userToken = authService.getUserToken();
  const adminToken = authService.getAdminToken();

  // Determine which token to use based on URL
  const token = config.url?.includes('/admin') ? adminToken : userToken;

  if (token && !config.headers) {
    config.headers = {};
  }

  if (token && config.headers) {
    console.log('[Interceptor] Adding user token from localStorage for:', config.url);
    (config.headers as Record<string, string>)['Authorization'] = `Bearer ${token}`;
  }

  return config;
});

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
  console.log('%c✓ Authentication Service initialized', 'color: #00aa00; font-weight: bold');
}

export default authService;
