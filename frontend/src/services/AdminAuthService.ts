// AdminAuthService
import { ApiResponse, ApiError } from '../types';
import {
  AdminBootstrapRequest,
  AdminLoginRequest,
  AdminVerify2FARequest,
  AdminSetup2FARequest,
  AdminRegisterViaInviteRequest,
  AdminDisable2FARequest,
  AdminRegenerateBackupCodesRequest,
  AdminInviteRequest,
  AdminRegistrationResponse,
  AdminLoginResponse,
  AdminAuthSuccessResponse,
  AdminTwoFactorSetupResponse,
  AdminTwoFactorCompleteResponse,
  AdminInvitationResponse,
} from '../types/adminAuth';

const API_BASE_URL = process.env.REACT_APP_API_URL || '/api';

class AdminAuthService {
  private async request<T>(
    endpoint: string,
    options?: RequestInit,
    token?: string
  ): Promise<ApiResponse<T>> {
    try {
      const headers: HeadersInit = {
        'Content-Type': 'application/json',
        ...options?.headers,
      };

      if (token) {
        headers['Authorization'] = `Bearer ${token}`;
      }

      const response = await fetch(`${API_BASE_URL}${endpoint}`, {
        ...options,
        headers,
      });

      const rawBody = await response.text();
      let parsedBody: any = null;

      if (rawBody) {
        try {
          parsedBody = JSON.parse(rawBody);
        } catch {
          parsedBody = rawBody;
        }
      }

      if (!response.ok) {
        const apiError: ApiError = {
          message: parsedBody?.message || `HTTP error ${response.status}`,
          status: response.status,
        };
        return { error: apiError };
      }

      return { data: parsedBody as T };
    } catch (error) {
      return {
        error: {
          message: error instanceof Error ? error.message : 'Unknown error occurred',
        },
      };
    }
  }

  // 1. Bootstrap Super Admin (one-time)
  async adminBootstrap(request: AdminBootstrapRequest): Promise<ApiResponse<AdminRegistrationResponse>> {
    return this.request<AdminRegistrationResponse>('/auth/admin/bootstrap', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  // 2. Admin Login (Step 1 - Password)
  async adminLogin(request: AdminLoginRequest): Promise<ApiResponse<AdminLoginResponse>> {
    return this.request<AdminLoginResponse>('/auth/admin-login', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  // 3. Verify 2FA (Step 2 - Code)
  async adminVerify2FA(
    request: AdminVerify2FARequest,
    tempToken: string
  ): Promise<ApiResponse<AdminAuthSuccessResponse>> {
    return this.request<AdminAuthSuccessResponse>(
      '/auth/admin/verify-2fa',
      {
        method: 'POST',
        body: JSON.stringify(request),
      },
      tempToken
    );
  }

  // 4. Generate 2FA Setup (QR Code)
  async adminGenerateSetup2FA(token: string): Promise<ApiResponse<AdminTwoFactorSetupResponse>> {
    return this.request<AdminTwoFactorSetupResponse>(
      '/auth/admin/setup-2fa/generate',
      { method: 'GET' },
      token
    );
  }

  // 5. Enable 2FA (Confirm QR Scan)
  async adminEnableSetup2FA(
    request: AdminSetup2FARequest,
    token: string
  ): Promise<ApiResponse<AdminTwoFactorCompleteResponse>> {
    return this.request<AdminTwoFactorCompleteResponse>(
      '/auth/admin/setup-2fa/enable',
      {
        method: 'POST',
        body: JSON.stringify(request),
      },
      token
    );
  }

  // 6. Disable 2FA
  async adminDisable2FA(
    request: AdminDisable2FARequest,
    token: string
  ): Promise<ApiResponse<any>> {
    return this.request(
      '/auth/admin/setup-2fa/disable',
      {
        method: 'POST',
        body: JSON.stringify(request),
      },
      token
    );
  }

  // 7. Regenerate Backup Codes
  async adminRegenerateBackupCodes(
    request: AdminRegenerateBackupCodesRequest,
    token: string
  ): Promise<ApiResponse<AdminTwoFactorCompleteResponse>> {
    return this.request<AdminTwoFactorCompleteResponse>(
      '/auth/admin/backup-codes/regenerate',
      {
        method: 'POST',
        body: JSON.stringify(request),
      },
      token
    );
  }

  // 8. Register Admin via Invitation
  async adminRegisterViaInvite(
    request: AdminRegisterViaInviteRequest
  ): Promise<ApiResponse<AdminRegistrationResponse>> {
    return this.request<AdminRegistrationResponse>('/auth/admin/register', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  }

  // 9. Invite New Admin
  async adminInvite(
    request: AdminInviteRequest,
    token: string
  ): Promise<ApiResponse<AdminInvitationResponse>> {
    return this.request<AdminInvitationResponse>(
      '/auth/admin/invite',
      {
        method: 'POST',
        body: JSON.stringify(request),
      },
      token
    );
  }
}

export const adminAuthService = new AdminAuthService();
