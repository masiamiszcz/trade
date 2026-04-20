// Admin Service
import { 
  AdminRequest, 
  Instrument, 
  HealthStatus, 
  AuditLog, 
  AdminUser, 
  PaginatedResponse,
  PaginationParams 
} from '../types/admin';

const API_URL = process.env.REACT_APP_API_URL || '/api';
const ADMIN_SESSION_KEY = 'trading-admin-session';

/**
 * Get admin token from session storage
 * Reads from trading-admin-session context, not user token
 */
function getAdminToken(): string | null {
  const session = localStorage.getItem(ADMIN_SESSION_KEY);
  
  console.log('🔍 [getAdminToken] localStorage key:', ADMIN_SESSION_KEY);
  console.log('🔍 [getAdminToken] session value:', session ? '✓ EXISTS' : '✗ MISSING');
  
  if (!session) {
    console.error('❌ [getAdminToken] No session found in localStorage. Available keys:', Object.keys(localStorage));
    return null;
  }
  
  try {
    const parsed = JSON.parse(session);
    console.log('🔍 [getAdminToken] parsed session:', {
      hasToken: !!parsed.token,
      tokenLength: parsed.token?.length || 0,
      sessionId: parsed.sessionId,
      adminId: parsed.adminId,
      username: parsed.username,
      isTempToken: parsed.isTempToken
    });
    const token = parsed.token || null;
    if (!token) {
      console.error('❌ [getAdminToken] Token is null/undefined in parsed session');
    } else {
      console.log('✅ [getAdminToken] Token extracted successfully, length:', token.length);
    }
    return token;
  } catch (e) {
    console.error('❌ [getAdminToken] Failed to parse session JSON:', e);
    return null;
  }
}

/**
 * Check if JWT token is expired
 * Decodes payload and compares exp claim with current time
 */
function isTokenExpired(token: string): boolean {
  try {
    // JWT format: header.payload.signature
    const parts = token.split('.');
    if (parts.length !== 3) return true;
    
    // Decode payload (base64url)
    const payload = JSON.parse(atob(parts[1]));
    
    // exp is in seconds, Date.now() is in milliseconds
    const currentTimeInSeconds = Math.floor(Date.now() / 1000);
    const expTime = payload.exp;
    
    // Add 5 second buffer for clock skew
    return currentTimeInSeconds >= (expTime - 5);
  } catch {
    return true; // If decoding fails, treat as expired
  }
}

class AdminService {
  // ===== HEALTH CHECK =====
  async getHealth(): Promise<HealthStatus> {
    const token = getAdminToken();
    // Health check endpoint is [AllowAnonymous] - can work without token
    // But include token if available for authenticated health status
    const headers: HeadersInit = {
      'Content-Type': 'application/json'
    };
    if (token) {
      headers['Authorization'] = `Bearer ${token}`;
      console.log('📤 [getHealth] Sending request with token');
    } else {
      console.log('⚠️  [getHealth] No token available - sending unauthenticated request');
    }
    
    console.log('📤 [getHealth] Sending request to /admin/health');
    const response = await fetch(`${API_URL}/admin/health`, { headers });
    console.log('📥 [getHealth] Response status:', response.status);
    
    if (!response.ok) {
      const errorText = await response.text();
      console.error('❌ [getHealth] API error:', response.status, errorText);
      throw new Error(`Health check failed: ${response.status}`);
    }
    return response.json();
  }

  // ===== ADMIN REQUESTS (Approvals) =====
  async getAdminRequests(params?: PaginationParams): Promise<PaginatedResponse<AdminRequest>> {
    const token = getAdminToken();
    if (!token) {
      console.error('❌ [getAdminRequests] No token available');
      throw new Error('Admin not authenticated');
    }
    
    const queryParams = new URLSearchParams();
    if (params) {
      queryParams.append('page', params.page.toString());
      queryParams.append('pageSize', params.pageSize.toString());
      if (params.sortBy) queryParams.append('sortBy', params.sortBy);
      if (params.sortOrder) queryParams.append('sortOrder', params.sortOrder);
    }

    console.log('📤 [getAdminRequests] Sending request to /admin/requests');
    const response = await fetch(`${API_URL}/admin/requests?${queryParams}`, {
      headers: { 
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      }
    });
    console.log('📥 [getAdminRequests] Response status:', response.status);
    
    if (!response.ok) {
      const errorText = await response.text();
      console.error('❌ [getAdminRequests] API error:', response.status, errorText);
      throw new Error('Failed to fetch requests');
    }
    return response.json();
  }

  async approveRequest(id: string, reason: string): Promise<AdminRequest> {
    const token = getAdminToken();
    if (!token) throw new Error('Admin not authenticated');
    const response = await fetch(`${API_URL}/admin/requests/${id}/approve`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({ reason })
    });
    if (!response.ok) throw new Error('Failed to approve request');
    return response.json();
  }

  async rejectRequest(id: string, reason: string): Promise<AdminRequest> {
    const token = getAdminToken();
    if (!token) throw new Error('Admin not authenticated');
    const response = await fetch(`${API_URL}/admin/requests/${id}/reject`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({ reason })
    });
    if (!response.ok) throw new Error('Failed to reject request');
    return response.json();
  }

  // ===== INSTRUMENTS =====
  async getInstruments(params?: PaginationParams): Promise<PaginatedResponse<Instrument>> {
    const token = getAdminToken();
    if (!token) throw new Error('Admin not authenticated');
    const queryParams = new URLSearchParams();
    if (params) {
      queryParams.append('page', params.page.toString());
      queryParams.append('pageSize', params.pageSize.toString());
      if (params.sortBy) queryParams.append('sortBy', params.sortBy);
      if (params.sortOrder) queryParams.append('sortOrder', params.sortOrder);
    }

    const response = await fetch(`${API_URL}/admin/instruments?${queryParams}`, {
      headers: { 'Authorization': `Bearer ${token}` }
    });
    if (!response.ok) throw new Error('Failed to fetch instruments');
    return response.json();
  }

  async createInstrument(data: Partial<Instrument>): Promise<Instrument> {
    const token = getAdminToken();
    if (!token) throw new Error('Admin not authenticated');
    const response = await fetch(`${API_URL}/admin/instruments`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to create instrument');
    return response.json();
  }

  async updateInstrument(id: string, data: Partial<Instrument>): Promise<Instrument> {
    const token = getAdminToken();
    if (!token) throw new Error('Admin not authenticated');
    const response = await fetch(`${API_URL}/admin/instruments/${id}`, {
      method: 'PUT',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to update instrument');
    return response.json();
  }

  async deleteInstrument(id: string): Promise<void> {
    const token = getAdminToken();
    if (!token) throw new Error('Admin not authenticated');
    const response = await fetch(`${API_URL}/admin/instruments/${id}`, {
      method: 'DELETE',
      headers: { 'Authorization': `Bearer ${token}` }
    });
    if (!response.ok) throw new Error('Failed to delete instrument');
  }

  async submitInstrumentForApproval(id: string, reason: string): Promise<AdminRequest> {
    const token = getAdminToken();
    if (!token) throw new Error('Admin not authenticated');
    const response = await fetch(`${API_URL}/admin/instruments/${id}/submit-approval`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({ reason })
    });
    if (!response.ok) throw new Error('Failed to submit instrument for approval');
    return response.json();
  }

  // ===== AUDIT LOGS =====
  async getAuditLogs(params: PaginationParams): Promise<PaginatedResponse<AuditLog>> {
    const token = getAdminToken();
    if (!token) throw new Error('Admin not authenticated');
    const queryParams = new URLSearchParams();
    queryParams.append('page', params.page.toString());
    queryParams.append('pageSize', params.pageSize.toString());
    if (params.sortBy) queryParams.append('sortBy', params.sortBy);
    if (params.sortOrder) queryParams.append('sortOrder', params.sortOrder);

    const response = await fetch(`${API_URL}/admin/audit-logs?${queryParams}`, {
      headers: { 'Authorization': `Bearer ${token}` }
    });
    if (!response.ok) throw new Error('Failed to fetch audit logs');
    return response.json();
  }

  // ===== USERS MANAGEMENT =====
  async getAdminUsers(): Promise<AdminUser[]> {
    const token = getAdminToken();
    if (!token) throw new Error('Admin not authenticated');
    const response = await fetch(`${API_URL}/admin/users`, {
      headers: { 'Authorization': `Bearer ${token}` }
    });
    if (!response.ok) throw new Error('Failed to fetch users');
    return response.json();
  }

  async changeUserRole(userId: string, newRole: string): Promise<AdminUser> {
    const token = getAdminToken();
    if (!token) throw new Error('Admin not authenticated');
    const response = await fetch(`${API_URL}/admin/users/${userId}/role`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({ role: newRole })
    });
    if (!response.ok) throw new Error('Failed to change user role');
    return response.json();
  }
}

export const adminService = new AdminService();
export { isTokenExpired };
