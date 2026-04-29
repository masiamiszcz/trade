/**
 * API Configuration - COMPLETE ENDPOINT REGISTRY
 * Centralized configuration for ALL API endpoints
 * Supports multiple environments: development, staging, production
 * 
 * Organized by functional area:
 * - health: System health checks
 * - account: User account management
 * - auth: User authentication
 * - adminAuth: Admin authentication (separate from user auth)
 * - instruments: User-facing instrument endpoints (GET, POST, PATCH, DELETE)
 * - admin.instruments: Admin instrument management
 * - admin.requests: Admin approval workflow
 * - admin.users: Admin user management
 * - admin.audit: Admin audit logging
 * 
 * Environment Variables:
 * - REACT_APP_API_BASE_URL: Base URL for API (default: /api for same-origin)
 * - REACT_APP_API_TIMEOUT: Request timeout in ms (default: 30000)
 * - REACT_APP_LOG_REQUESTS: Enable request logging (default: true in dev, false in prod)
 */

export const API_CONFIG = {
  // Base URL for all API requests - uses relative path for nginx routing
  baseUrl: process.env.REACT_APP_API_BASE_URL || '/api',
  
  // Request timeout in milliseconds
  timeout: parseInt(process.env.REACT_APP_API_TIMEOUT || '30000', 10),
  
  // Enable detailed request/response logging
  logging: process.env.REACT_APP_LOG_REQUESTS !== 'false',
  
  // Enable automatic retry on failure
  enableRetry: true,
  
  // Maximum number of retry attempts
  maxRetries: 3,
  
  // Retry delay multiplier (exponential backoff)
  retryDelayMs: 1000,
  
  // HTTP status codes that should trigger retry
  retryableStatusCodes: [408, 429, 500, 502, 503, 504],
  
  // API version (can be incremented for backwards compatibility)
  version: 'v1',
  
  // Headers sent with every request
  defaultHeaders: {
    'Content-Type': 'application/json',
  },
  
  // ============================================================
  // COMPLETE ENDPOINT MAPPING (All backend endpoints)
  // ============================================================
  endpoints: {
    // ==================== HEALTH & INFO ====================
    health: {
      public: '/health',
      info: '/info',
      admin: '/auth/admin/health',
    },

    // ==================== ACCOUNT ====================
    account: {
      profile: '/account',
      main: '/account/main',
    },

    // ==================== USER AUTHENTICATION ====================
    auth: {
      login: '/user/login',
      register: '/user/register',
      logout: '/user/logout',
    },

    // ==================== ADMIN AUTHENTICATION ====================
    adminAuth: {
      bootstrap: '/auth/admin/bootstrap',
      register: '/auth/admin/register',
      login: '/auth/admin-login',
      invite: '/auth/admin/invite',
      verify2fa: '/auth/admin/verify-2fa',
      backup2faRegenerate: '/auth/admin/backup-codes/regenerate',
      setup2faGenerate: '/auth/admin/setup-2fa/generate',
      setup2faEnable: '/auth/admin/setup-2fa/enable',
      setup2faDisable: '/auth/admin/setup-2fa/disable',
    },

    // ==================== MARKET DATA ====================
    market: {
      all: '/market',
      bySymbol: (symbol: string) => `/market/${symbol}`,
    },

    // ==================== USER INSTRUMENTS (Public/Read-Only Endpoints) ====================
    instruments: {
      // GET endpoints (read-only, no approval needed)
      all: '/instruments',
      active: '/admin/instruments/active',
      byId: (id: string) => `/instruments/${id}`,
      bySymbol: (symbol: string) => `/instruments/symbol/${symbol}`,
      
      // POST/PATCH/DELETE endpoints (require approval workflow)
      create: '/instruments',
      requestApproval: (id: string) => `/instruments/${id}/request-approval`,
      approve: (id: string) => `/instruments/${id}/approve`,
      reject: (id: string) => `/instruments/${id}/reject`,
      retrySubmission: (id: string) => `/instruments/${id}/retry-submission`,
      archive: (id: string) => `/instruments/${id}/archive`,
      block: (id: string) => `/instruments/${id}/block`,
      unblock: (id: string) => `/instruments/${id}/unblock`,
      delete: (id: string) => `/instruments/${id}`,
    },
    
    // ==================== CRYPTO SPECIFIC ====================
    crypto: {
      instruments: '/crypto/cryptoinstruments',
      candlesBySymbol: (symbol: string) => `/crypto/${symbol}/candles`,
    },

    // ==================== ADMIN: INSTRUMENTS ====================
        adminInstruments: {
    // GET endpoints
    all: '/admin/instruments',
    byId: (id: string) => `/admin/instruments/${id}`,
    active: '/admin/instruments/active',
    bySymbol: (symbol: string) => `/admin/instruments/symbol/${symbol}`,

    // CREATE
    create: '/admin/instruments',

    // UPDATE (approval flow)
    requestUpdate: (id: string) => `/admin/instruments/${id}`,

    // BLOCK / UNBLOCK (approval flow)
    requestBlock: (id: string) => `/admin/instruments/${id}/block`,
    requestUnblock: (id: string) => `/admin/instruments/${id}/unblock`,

    // DELETE (approval flow)
    requestDelete: (id: string) => `/admin/instruments/${id}`,

    // APPROVAL
    requestApproval: (id: string) => `/admin/instruments/${id}/request-approval`,
    },

    adminRequests: {
    all: '/admin/approvals',
    pending: '/admin/approvals/pending',
    byId: (id: string) => `/admin/approvals/${id}`,
    approve: (id: string) => `/admin/approvals/${id}/approve`,
    reject: (id: string) => `/admin/approvals/${id}/reject`,
    },

    // ==================== ADMIN: USER MANAGEMENT ====================
    adminUsers: {
      all: '/admin/users',
    },

    // ==================== ADMIN: AUDIT LOGGING ====================
    adminAudit: {
      byEntity: (entityType: string, entityId: string) => `/admin/audit-logs/entity/${entityType}/${entityId}`,
      history: '/admin/audit-history',
    },
  },
} as const;

// Type-safe endpoint getter for complex cases
export const getEndpoint = <T extends keyof typeof API_CONFIG.endpoints>(
  section: T,
  key: keyof typeof API_CONFIG.endpoints[T]
): string => {
  const endpoint = API_CONFIG.endpoints[section][key];
  return typeof endpoint === 'function' ? endpoint : (endpoint as string);
};

export type ApiConfigType = typeof API_CONFIG;
