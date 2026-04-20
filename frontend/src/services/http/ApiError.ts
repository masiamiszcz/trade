/**
 * Extended API Error Class
 * Provides rich error information for debugging and user feedback
 */

export class ApiError extends Error {
  constructor(
    message: string,
    public status?: number,
    public requestId?: string,
    public details?: unknown
  ) {
    super(message);
    this.name = 'ApiError';

    // Maintain proper prototype chain for instanceof checks
    Object.setPrototypeOf(this, ApiError.prototype);
  }

  /**
   * Get user-friendly error message
   */
  getUserMessage(): string {
    if (this.status === 401) {
      return 'Unauthorized - please log in again';
    }
    if (this.status === 403) {
      return 'Forbidden - you do not have permission';
    }
    if (this.status === 404) {
      return 'Resource not found';
    }
    if (this.status === 429) {
      return 'Too many requests - please try again later';
    }
    if (this.status === 500) {
      return 'Server error - please try again later';
    }
    if (this.status && this.status >= 500) {
      return 'Server error - please try again later';
    }
    if (this.status && this.status >= 400) {
      return 'Request error - please check your input';
    }
    return this.message || 'Unknown error occurred';
  }

  /**
   * Get error details for debugging
   */
  getDetails(): Record<string, unknown> {
    return {
      message: this.message,
      status: this.status,
      requestId: this.requestId,
      details: this.details,
      timestamp: new Date().toISOString(),
    };
  }

  /**
   * Convert to JSON for logging
   */
  toJSON(): Record<string, unknown> {
    return this.getDetails();
  }
}

/**
 * Check if error is ApiError
 */
export function isApiError(error: unknown): error is ApiError {
  return error instanceof ApiError;
}

/**
 * Get error message from unknown error
 */
export function getErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    return error.message;
  }
  if (error instanceof Error) {
    return error.message;
  }
  if (typeof error === 'string') {
    return error;
  }
  return 'Unknown error occurred';
}

/**
 * Get user-friendly message from unknown error
 */
export function getUserErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    return error.getUserMessage();
  }
  return 'An error occurred. Please try again.';
}
