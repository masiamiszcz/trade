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
   * Store user token
   */
  setUserToken(token: string): void {
    localStorage.setItem(TOKEN_STORAGE_KEY, token);
  }

  /**
   * Store admin token
   */
  setAdminToken(token: string): void {
    localStorage.setItem(ADMIN_TOKEN_STORAGE_KEY, token);
  }

  /**
   * Clear user token
   */
  clearUserToken(): void {
    localStorage.removeItem(TOKEN_STORAGE_KEY);
  }

  /**
   * Clear admin token
   */
  clearAdminToken(): void {
    localStorage.removeItem(ADMIN_TOKEN_STORAGE_KEY);
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
  const userToken = authService.getUserToken();
  const adminToken = authService.getAdminToken();

  // Determine which token to use based on URL
  const token = config.url?.includes('/admin') ? adminToken : userToken;

  if (token && !config.headers) {
    config.headers = {};
  }

  if (token && config.headers) {
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
