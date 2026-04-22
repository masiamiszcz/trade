/**
 * HTTP Client with Interceptors
 * Handles all HTTP communication with request/response interceptors,
 * error handling, retry logic, and request logging
 * 
 * Features:
 * - Request/Response interceptors for middleware-like functionality
 * - Exponential backoff retry strategy
 * - Request/Response logging for debugging
 * - Centralized error handling
 * - Request timeout handling
 * - Automatic token injection in Authorization header (2FA aware)
 * - Intelligent token management (temp vs final)
 */

import { API_CONFIG } from '../../config/apiConfig';
import { ApiError } from './ApiError';

/**
 * Token storage configuration
 * Supports multiple auth contexts (user + admin)
 */
interface TokenStorage {
  // User tokens
  finalUserToken: string | null;           // 'auth_token'
  tempUserToken: string | null;            // 'trading-platform-temp-token'
  tempUserSessionId: string | null;        // 'trading-platform-session-id'

  // Admin tokens (stored as single JSON object)
  adminSession: AdminSessionStorage | null; // 'trading-admin-session'
}

interface AdminSessionStorage {
  token: string;
  sessionId?: string;
  adminId?: string;
  username?: string;
  email?: string;
  isTempToken?: boolean;
  requiresTwoFactor?: boolean;
  isSuperAdmin?: boolean;
}

/**
 * Request interceptor types
 */
export interface RequestInterceptor {
  (config: RequestConfig): Promise<RequestConfig> | RequestConfig;
}

/**
 * Response interceptor types
 */
export interface ResponseInterceptor {
  (response: Response): Promise<Response> | Response;
}

/**
 * Error interceptor types
 */
export interface ErrorInterceptor {
  (error: Error): Promise<void> | void;
}

/**
 * Request configuration
 */
export interface RequestConfig extends RequestInit {
  url: string;
  retryCount?: number;
}

/**
 * HTTP Client with interceptor support
 */
class HttpClient {
  private requestInterceptors: RequestInterceptor[] = [];
  private responseInterceptors: ResponseInterceptor[] = [];
  private errorInterceptors: ErrorInterceptor[] = [];
  private requestLog: Map<string, RequestLogEntry> = new Map();

  /**
   * CRITICAL: Get authentication token based on request endpoint
   * 
   * Token Selection Rules:
   * RULE 1 - FINAL TOKEN: For regular API calls
   *   Use final token if exists (admin or user)
   * 
   * RULE 2 - TEMP TOKEN: For 2FA verification endpoints
   *   Use temp token for: /verify-2fa, /verify-login-2fa, /register-complete-2fa
   *   
   * RULE 3 - POST 2FA SUCCESS: Response contains final token
   *   Save as main token, clear temp token
   * 
   * @param endpoint - API endpoint URL (e.g., '/user/verify-2fa' or 'http://localhost/api/user/verify-2fa')
   * @returns Token string or null if not authenticated
   */
  private getAuthToken(endpoint: string): string | null {
    // Normalize endpoint - extract just the path if full URL is passed
    const path = endpoint.includes('/api/') 
      ? endpoint.substring(endpoint.indexOf('/api/'))
      : endpoint.includes('://')
      ? new URL(endpoint).pathname
      : endpoint;

    // Parse admin session if exists
    let adminSession: AdminSessionStorage | null = null;
    try {
      const stored = localStorage.getItem('trading-admin-session');
      if (stored) {
        adminSession = JSON.parse(stored);
      }
    } catch {
      // Ignore parse errors
    }

    // 2FA verification endpoints require TEMP TOKEN
    const is2FAEndpoint =
      path.includes('/verify-2fa') ||
      path.includes('/verify-login-2fa') ||
      path.includes('/register-complete-2fa') ||
      path.includes('/setup-2fa/generate') ||
      path.includes('/setup-2fa/enable');

    if (is2FAEndpoint) {
      // Check for user temp token first
      const tempToken = localStorage.getItem('trading-platform-temp-token');
      if (tempToken) {
        console.log('[HttpClient] Using TEMP TOKEN for 2FA endpoint:', path);
        return tempToken;
      }

      // For admin, temp token is stored in admin session
      if (adminSession?.isTempToken && adminSession?.token) {
        console.log('[HttpClient] Using ADMIN TEMP TOKEN for 2FA endpoint:', path);
        return adminSession.token;
      }
    }

    // FINAL TOKEN usage - regular API calls
    // Priority: Admin final token > User final token
    if (adminSession?.token && !adminSession.isTempToken) {
      console.log('[HttpClient] Using ADMIN FINAL TOKEN for:', path);
      return adminSession.token;
    }

    const userToken = localStorage.getItem('auth_token');
    if (userToken) {
      console.log('[HttpClient] Using USER FINAL TOKEN for:', path);
      return userToken;
    }

    // No token available
    console.log('[HttpClient] No token available for:', path);
    return null;
  }

  /**
   * Add authorization header automatically if token exists
   * This is the REQUEST INTERCEPTOR for token injection
   */
  private injectAuthorizationHeader(config: RequestConfig): RequestConfig {
    // 🔐 SECURITY: Protected 2FA endpoints that REQUIRE tokens (even during registration/login)
    // These must be checked BEFORE public endpoints to avoid false positives
    const protected2FAEndpoints = [
      '/verify-2fa',
      '/verify-login-2fa',
      '/register-complete-2fa',
      '/register/verify-2fa',  // ← User registration 2FA verification
      '/setup-2fa/generate',
      '/setup-2fa/enable',
    ];

    const is2FAProtected = protected2FAEndpoints.some(endpoint =>
      config.url?.includes(endpoint)
    );

    // Don't inject token for public endpoints (that are NOT 2FA protected)
    const publicEndpoints = [
      '/auth/login',
      '/auth/register',
      '/user/login',
      '/user/register',
      '/admin/bootstrap',
      '/health',
      '/market',
    ];

    const isPublic = !is2FAProtected && publicEndpoints.some(endpoint =>
      config.url?.includes(endpoint)
    );

    if (isPublic) {
      return config;
    }

    // Get appropriate token based on endpoint
    const token = this.getAuthToken(config.url || '');
    
    if (token) {
      // Remove existing Authorization header if present (to avoid duplicates)
      if (config.headers && typeof config.headers === 'object' && !Array.isArray(config.headers)) {
        delete (config.headers as Record<string, unknown>)['Authorization'];
      }

      // Add Authorization header
      config.headers = {
        ...config.headers,
        'Authorization': `Bearer ${token}`,
      };

      console.log('[HttpClient] ✓ Authorization header injected for:', config.url);
    }

    return config;
  }

  /**
   * Add request interceptor
   */
  addRequestInterceptor(interceptor: RequestInterceptor): void {
    this.requestInterceptors.push(interceptor);
  }

  /**
   * Add response interceptor
   */
  addResponseInterceptor(interceptor: ResponseInterceptor): void {
    this.responseInterceptors.push(interceptor);
  }

  /**
   * Add error interceptor
   */
  addErrorInterceptor(interceptor: ErrorInterceptor): void {
    this.errorInterceptors.push(interceptor);
  }

  /**
   * Execute request interceptors chain
   * FIRST: Automatically inject authorization header
   * THEN: Run user-defined interceptors
   */
  private async executeRequestInterceptors(
    config: RequestConfig
  ): Promise<RequestConfig> {
    // STEP 1: Automatic token injection (built-in interceptor)
    let processedConfig = this.injectAuthorizationHeader(config);

    // STEP 2: Run user-defined interceptors
    for (const interceptor of this.requestInterceptors) {
      processedConfig = await Promise.resolve(interceptor(processedConfig));
    }

    return processedConfig;
  }

  /**
   * Execute response interceptors chain
   */
  private async executeResponseInterceptors(
    response: Response
  ): Promise<Response> {
    let processedResponse = response;

    for (const interceptor of this.responseInterceptors) {
      processedResponse = await Promise.resolve(interceptor(processedResponse));
    }

    return processedResponse;
  }

  /**
   * Execute error interceptors chain
   */
  private async executeErrorInterceptors(error: Error): Promise<void> {
    for (const interceptor of this.errorInterceptors) {
      await Promise.resolve(interceptor(error));
    }
  }

  /**
   * Log request with unique ID
   */
  private logRequest(
    method: string,
    url: string,
    requestId: string,
    headers: HeadersInit,
    body?: unknown
  ): void {
    if (!API_CONFIG.logging) return;

    const logEntry: RequestLogEntry = {
      requestId,
      timestamp: new Date().toISOString(),
      method,
      url,
      status: 'pending',
    };

    this.requestLog.set(requestId, logEntry);

    console.log(
      `%c[${requestId}] ${method} ${url}`,
      'color: #0066cc; font-weight: bold'
    );
    
    if (body && method !== 'GET') {
      console.log(
        `%c[${requestId}] Request body:`,
        'color: #0066cc',
        body
      );
    }
  }

  /**
   * Log response
   */
  private logResponse(
    requestId: string,
    status: number,
    headers: Headers,
    responseBody: unknown,
    duration: number
  ): void {
    if (!API_CONFIG.logging) return;

    const logEntry = this.requestLog.get(requestId);
    if (logEntry) {
      logEntry.status = status >= 200 && status < 300 ? 'success' : 'error';
      logEntry.statusCode = status;
      logEntry.duration = duration;
    }

    const statusColor = status >= 200 && status < 300 ? '#00aa00' : '#cc0000';
    console.log(
      `%c[${requestId}] ✓ ${status} (${duration}ms)`,
      `color: ${statusColor}; font-weight: bold`
    );
    
    if (responseBody) {
      console.log(
        `%c[${requestId}] Response:`,
        `color: ${statusColor}`,
        responseBody
      );
    }
  }

  /**
   * Handle fetch errors with retry logic
   */
  private async handleFetchError(
    error: Error,
    config: RequestConfig,
    retryCount: number = 0
  ): Promise<Response> {
    const requestId = this.getRequestId(config);

    // Execute error interceptors
    await this.executeErrorInterceptors(error);

    if (!API_CONFIG.enableRetry) {
      throw error;
    }

    if (retryCount >= API_CONFIG.maxRetries) {
      console.error(
        `%c[${requestId}] ✗ Failed after ${retryCount} retries`,
        'color: #cc0000; font-weight: bold',
        error
      );
      throw error;
    }

    // Exponential backoff: 1s, 2s, 4s, etc.
    const delay = API_CONFIG.retryDelayMs * Math.pow(2, retryCount);
    
    console.warn(
      `%c[${requestId}] ⟳ Retrying (${retryCount + 1}/${API_CONFIG.maxRetries}) after ${delay}ms`,
      'color: #ff6600; font-weight: bold'
    );

    await new Promise((resolve) => setTimeout(resolve, delay));

    return this.fetch(
      {
        ...config,
        retryCount: retryCount + 1,
      }
    );
  }

  /**
   * Generate unique request ID
   */
  private getRequestId(config: RequestConfig): string {
    return config.requestId || `REQ-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
  }

  /**
   * Main fetch method with interceptors and retry logic
   */
  async fetch<T = unknown>(config: RequestConfig): Promise<T> {
    const startTime = performance.now();
    const requestId = this.getRequestId(config);
    config.requestId = requestId;

    try {
      // IMPORTANT: Store original endpoint BEFORE building full URL
      // This is used by getAuthToken() to detect 2FA endpoints
      const originalEndpoint = config.url;

      // Run request interceptors (BEFORE building full URL)
      // This allows token injection based on original endpoint path
      let processedConfig = await this.executeRequestInterceptors(config);

      // Now build full URL using potentially modified config.url
      const fullUrl = `${API_CONFIG.baseUrl}${processedConfig.url}`;

      // Set timeout
      const controller = new AbortController();
      const timeoutId = setTimeout(() => controller.abort(), API_CONFIG.timeout);

      // Log request
      this.logRequest(
        processedConfig.method || 'GET',
        fullUrl,
        requestId,
        processedConfig.headers || {},
        processedConfig.body
      );

      // Perform fetch
      let response = await fetch(fullUrl, {
        ...processedConfig,
        signal: controller.signal,
      });

      clearTimeout(timeoutId);

      // Run response interceptors
      response = await this.executeResponseInterceptors(response);

      // Parse response
      const contentType = response.headers.get('content-type');
      let responseBody: unknown;

      if (contentType?.includes('application/json')) {
        responseBody = await response.json();
      } else {
        responseBody = await response.text();
      }

      const duration = Math.round(performance.now() - startTime);

      // Log response
      this.logResponse(requestId, response.status, response.headers, responseBody, duration);

      // Check if response is ok
      if (!response.ok) {
        const error = new ApiError(
          this.parseErrorMessage(responseBody),
          response.status,
          requestId,
          responseBody
        );

        throw error;
      }

      return responseBody as T;
    } catch (error) {
      // Handle specific error types
      if (error instanceof TypeError && error.message.includes('Failed to fetch')) {
        // Network error
        return this.handleFetchError(
          new Error('Network error - check connection and CORS settings'),
          config,
          config.retryCount || 0
        );
      }

      if (error instanceof ApiError) {
        // Check if retryable
        if (
          API_CONFIG.enableRetry &&
          config.retryCount! < API_CONFIG.maxRetries &&
          API_CONFIG.retryableStatusCodes.includes(error.status)
        ) {
          return this.handleFetchError(error, config, (config.retryCount || 0) + 1);
        }
      }

      // Re-throw error
      throw error;
    }
  }

  /**
   * Parse error message from various response formats
   */
  private parseErrorMessage(responseBody: unknown): string {
    if (typeof responseBody === 'object' && responseBody !== null) {
      const body = responseBody as Record<string, unknown>;
      return (
        body.message ||
        body.error ||
        body.title ||
        JSON.stringify(body)
      ) as string;
    }

    if (typeof responseBody === 'string') {
      return responseBody;
    }

    return 'Unknown error occurred';
  }

  /**
   * Get request history for debugging
   */
  getRequestHistory(): RequestLogEntry[] {
    return Array.from(this.requestLog.values());
  }

  /**
   * Clear request history
   */
  clearRequestHistory(): void {
    this.requestLog.clear();
  }

  /**
   * Download request history as JSON for debugging
   */
  downloadRequestHistory(): void {
    const history = this.getRequestHistory();
    const dataStr = JSON.stringify(history, null, 2);
    const dataBlob = new Blob([dataStr], { type: 'application/json' });
    const url = URL.createObjectURL(dataBlob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `api-requests-${new Date().toISOString()}.json`;
    link.click();
    URL.revokeObjectURL(url);
  }
}

/**
 * Request log entry
 */
export interface RequestLogEntry {
  requestId: string;
  timestamp: string;
  method: string;
  url: string;
  status: 'pending' | 'success' | 'error';
  statusCode?: number;
  duration?: number;
}

// Export singleton instance
export const httpClient = new HttpClient();

// Enable logging in development
if (process.env.NODE_ENV === 'development') {
  console.log('%c🔧 HTTP Client initialized in development mode', 'color: #0066cc; font-weight: bold');
}
