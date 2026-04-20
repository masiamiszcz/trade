/**
 * API Configuration
 * Centralized configuration for all API endpoints
 * Supports multiple environments: development, staging, production
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
  
  // Endpoints mapping for type safety
  endpoints: {
    // Authentication
    auth: {
      login: '/auth/login',
      register: '/auth/register',
      logout: '/auth/logout',
      refresh: '/auth/refresh',
    },
    
    // User
    user: {
      profile: '/user/profile',
      settings: '/user/settings',
      accounts: '/user/accounts',
    },
    
    // Market Data
    market: {
      health: '/health',
      assets: '/market',
      asset: (symbol: string) => `/market/${symbol}`,
      instruments: '/market/instruments',
    },
    
    // Admin
    admin: {
      bootstrap: '/admin/bootstrap',
      login: '/admin/auth/login',
      verify2fa: '/admin/auth/2fa/verify',
      setup2fa: '/admin/auth/2fa/setup',
      invitations: '/admin/invitations',
      requests: '/admin/requests',
      health: '/admin/health',
    },
  },
} as const;

// Type-safe endpoint getter
export const getEndpoint = <T extends keyof typeof API_CONFIG.endpoints>(
  section: T,
  key: keyof typeof API_CONFIG.endpoints[T]
): string => {
  const endpoint = API_CONFIG.endpoints[section][key];
  return typeof endpoint === 'function' ? endpoint : (endpoint as string);
};

export type ApiConfigType = typeof API_CONFIG;
