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
 * - Token injection in Authorization header
 */

import { API_CONFIG } from '../../config/apiConfig';
import { ApiError } from './ApiError';

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
   */
  private async executeRequestInterceptors(
    config: RequestConfig
  ): Promise<RequestConfig> {
    let processedConfig = config;

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
      // Build full URL
      const fullUrl = `${API_CONFIG.baseUrl}${config.url}`;
      config.url = fullUrl;

      // Run request interceptors
      const processedConfig = await this.executeRequestInterceptors(config);

      // Set timeout
      const controller = new AbortController();
      const timeoutId = setTimeout(() => controller.abort(), API_CONFIG.timeout);

      // Log request
      this.logRequest(
        processedConfig.method || 'GET',
        config.url,
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
