# 🏗️ Enterprise-Grade HTTP Client Architecture

## Overview

This document describes the centralized, professional-grade HTTP client architecture for the Trading Platform Frontend. It's designed following enterprise software engineering best practices with focus on maintainability, scalability, debugging, and production reliability.

## Architecture Layers

```
┌─────────────────────────────────────────────────────┐
│          React Components/Pages                       │
├─────────────────────────────────────────────────────┤
│       AuthenticationService / MarketDataService      │
│              (Business Logic Layer)                  │
├─────────────────────────────────────────────────────┤
│              HTTP Client with Interceptors           │
│         (Request/Response Middleware Layer)          │
├─────────────────────────────────────────────────────┤
│    Configuration / Error Handling / Logging          │
│           (Infrastructure Layer)                     │
├─────────────────────────────────────────────────────┤
│    Fetch API / Network / Browser APIs                │
│           (Browser Runtime)                          │
└─────────────────────────────────────────────────────┘
```

## File Structure

```
frontend/src/
├── services/
│   ├── http/
│   │   ├── HttpClient.ts          # Core HTTP client with interceptors
│   │   ├── ApiError.ts            # Extended error class with rich info
│   │   └── index.ts               # Module exports
│   ├── AuthenticationService.ts    # Unified auth operations
│   ├── MarketDataService.ts        # Market data operations
│   ├── AuthService.ts             # [DEPRECATED] Use AuthenticationService
│   └── ApiService.ts              # [DEPRECATED] Use MarketDataService
├── config/
│   └── apiConfig.ts               # Centralized API configuration
├── types/
│   └── index.ts                   # Type definitions
└── ...
```

## Key Components

### 1. API Configuration (`config/apiConfig.ts`)

**Purpose:** Single source of truth for API configuration

**Features:**
- ✅ Environment-based configuration
- ✅ Endpoint mapping with type safety
- ✅ Timeout, retry, and logging settings
- ✅ Request/response defaults

**Example Usage:**
```typescript
import { API_CONFIG, getEndpoint } from '../config/apiConfig';

// Direct access to settings
const timeout = API_CONFIG.timeout; // 30000ms
const baseUrl = API_CONFIG.baseUrl; // /api

// Type-safe endpoint access
const loginUrl = API_CONFIG.endpoints.auth.login; // '/auth/login'
const healthUrl = getEndpoint('market', 'health'); // '/health'
```

### 2. HTTP Client (`services/http/HttpClient.ts`)

**Purpose:** Centralized HTTP communication with middleware support

**Key Features:**
- ✅ Request/Response/Error interceptors
- ✅ Exponential backoff retry logic
- ✅ Detailed request/response logging
- ✅ Request timeout handling
- ✅ Request ID tracking for distributed tracing
- ✅ Automatic token injection (via interceptors)

**Request Flow:**
```
1. User calls httpClient.fetch({ url, method, body })
2. Generate unique request ID
3. Execute request interceptors (e.g., add auth token)
4. Log request (method, URL, body)
5. Send HTTP request with timeout
6. Execute response interceptors
7. Parse response (JSON/text)
8. Log response (status, duration, body)
9. Return data or throw error
10. On error: execute error interceptors, retry if applicable
```

**Usage Examples:**
```typescript
import { httpClient } from '../services/http';

// Simple GET request
const data = await httpClient.fetch<HealthStatus>({
  url: '/health',
  method: 'GET',
});

// POST with body
const response = await httpClient.fetch<AuthResponse>({
  url: '/auth/login',
  method: 'POST',
  body: JSON.stringify({ email, password }),
});

// With custom headers
const result = await httpClient.fetch<UserProfile>({
  url: '/user/profile',
  method: 'GET',
  headers: {
    'X-Custom-Header': 'value',
  },
});
```

### 3. API Error (`services/http/ApiError.ts`)

**Purpose:** Rich error information for debugging and user feedback

**Features:**
- ✅ HTTP status code
- ✅ Request ID for tracing
- ✅ Error details/context
- ✅ User-friendly messages
- ✅ Developer debugging info

**Error Details:**
```typescript
try {
  await authService.userLogin(credentials);
} catch (error) {
  if (isApiError(error)) {
    // Get developer info
    console.log(error.getDetails());
    // {
    //   message: "Invalid credentials",
    //   status: 401,
    //   requestId: "REQ-1234567890-abc123def456",
    //   details: { /* server response */ },
    //   timestamp: "2026-04-20T10:30:45.123Z"
    // }
    
    // Get user-friendly message
    const userMsg = error.getUserMessage();
    // "Unauthorized - please log in again"
  }
}
```

### 4. Authentication Service (`services/AuthenticationService.ts`)

**Purpose:** Centralized user and admin authentication

**Features:**
- ✅ User login/register/logout
- ✅ Admin bootstrap/login/2FA
- ✅ Token management (storage/retrieval)
- ✅ Automatic token injection in requests
- ✅ Auto logout on 401

**Usage Examples:**
```typescript
import { authService } from '../services/AuthenticationService';

// User login
const response = await authService.userLogin({
  userNameOrEmail: 'user@example.com',
  password: 'password123',
});

// User register
const response = await authService.userRegister({
  userName: 'newuser',
  email: 'new@example.com',
  firstName: 'John',
  lastName: 'Doe',
  password: 'securePassword123',
  baseCurrency: 'PLN',
});

// Check authentication
if (authService.isUserAuthenticated()) {
  // User is logged in
}

// Get token
const token = authService.getUserToken();

// Logout
await authService.userLogout();
```

### 5. Market Data Service (`services/MarketDataService.ts`)

**Purpose:** Market data and health check operations

**Features:**
- ✅ Health status check
- ✅ Asset retrieval
- ✅ Instrument listing

**Usage Examples:**
```typescript
import { marketDataService } from '../services/MarketDataService';

// Health check
const health = await marketDataService.getHealth();

// Get all assets
const assets = await marketDataService.getAllAssets();

// Get specific asset
const asset = await marketDataService.getAssetBySymbol('AAPL');

// Get instruments
const instruments = await marketDataService.getInstruments();
```

## Interceptors

### Request Interceptors

Executed before sending request. Used for:
- Adding authorization tokens
- Adding common headers
- Modifying request body
- Request preprocessing

**Built-in Interceptor:** Token Injection
```typescript
// Automatically added during service initialization
httpClient.addRequestInterceptor((config) => {
  const token = authService.getUserToken();
  if (token) {
    if (!config.headers) config.headers = {};
    config.headers['Authorization'] = `Bearer ${token}`;
  }
  return config;
});
```

**Custom Interceptor Example:**
```typescript
httpClient.addRequestInterceptor((config) => {
  // Add custom header
  if (!config.headers) config.headers = {};
  config.headers['X-Request-ID'] = generateRequestId();
  return config;
});
```

### Response Interceptors

Executed after receiving response. Used for:
- Response transformation
- Status-specific handling
- Response validation

**Built-in Interceptor:** 401 Handler
```typescript
// Automatically added during service initialization
httpClient.addResponseInterceptor((response) => {
  if (response.status === 401) {
    authService.clearAllTokens();
    window.dispatchEvent(new CustomEvent('auth:unauthorized'));
  }
  return response;
});
```

### Error Interceptors

Executed on error. Used for:
- Error logging
- Error recovery
- Notifications

**Custom Interceptor Example:**
```typescript
httpClient.addErrorInterceptor((error) => {
  // Log to error tracking service
  errorTracker.captureException(error);
  
  // Notify user
  if (isApiError(error) && error.status === 500) {
    showNotification('Server error', 'error');
  }
});
```

## Retry Logic

**Strategy:** Exponential Backoff

**Configuration:**
```typescript
// From apiConfig.ts
enableRetry: true,
maxRetries: 3,
retryDelayMs: 1000, // 1s, 2s, 4s
retryableStatusCodes: [408, 429, 500, 502, 503, 504],
```

**Retry Timeline:**
```
Attempt 1: Immediate
  ↓ (Fails with 503)
Wait 1000ms
Attempt 2: 1s delay
  ↓ (Fails with 503)
Wait 2000ms
Attempt 3: 3s delay
  ↓ (Fails with 503)
Wait 4000ms
Attempt 4: 7s delay
  ↓ (Fails)
Throw error after 7s
```

## Request Logging

**Features:**
- ✅ Unique request ID per request
- ✅ Timestamp
- ✅ Method, URL, status
- ✅ Request/response body
- ✅ Duration
- ✅ Request history storage

**Console Output (Development):**
```
[REQ-1234567890-abc123def456] POST /api/auth/login
[REQ-1234567890-abc123def456] Request body: {email, password}
[REQ-1234567890-abc123def456] ✓ 200 (145ms)
[REQ-1234567890-abc123def456] Response: {token: "..."}
```

**Access Request History:**
```typescript
// Get all request logs
const history = httpClient.getRequestHistory();

// Download logs as JSON
httpClient.downloadRequestHistory();

// Clear history
httpClient.clearRequestHistory();
```

## Environment Configuration

### Development (`REACT_APP_LOG_REQUESTS=true`)
- ✅ Full request/response logging
- ✅ Verbose error messages
- ✅ Request history available
- ✅ Retry delays shorter

### Production (`REACT_APP_LOG_REQUESTS=false`)
- ✅ Minimal logging
- ✅ User-friendly error messages
- ✅ Performance optimized
- ✅ Standard retry delays

## Error Handling Patterns

### Pattern 1: Component-Level Error Handling
```typescript
const handleLogin = async (credentials: LoginRequest) => {
  try {
    await authService.userLogin(credentials);
    navigate('/dashboard');
  } catch (error) {
    if (isApiError(error)) {
      setErrorMessage(error.getUserMessage());
    } else {
      setErrorMessage('Unknown error occurred');
    }
  }
};
```

### Pattern 2: Hook-Level Error Handling
```typescript
const useLogin = () => {
  const [error, setError] = useState<ApiError | null>(null);
  
  const login = async (credentials: LoginRequest) => {
    try {
      return await authService.userLogin(credentials);
    } catch (err) {
      const apiError = err instanceof ApiError ? err : null;
      setError(apiError);
      throw err;
    }
  };
  
  return { login, error };
};
```

### Pattern 3: Error Boundary
```typescript
class ApiErrorBoundary extends React.Component {
  componentDidCatch(error: Error) {
    if (isApiError(error)) {
      console.error('API Error:', error.getDetails());
    }
  }
  
  render() {
    return this.props.children;
  }
}
```

## Network Routing Architecture

### Frontend → Nginx → Backend

```
User Browser (localhost:80)
        ↓
    Fetch /api/auth/login
        ↓
Nginx (localhost:80) [Public]
        ↓ (routes via backend-nginx network)
Backend (trading-backend:5001) [Internal]
        ↓
SQL Server (trading-sql:1433) [Internal]
```

**Benefits:**
- ✅ Same-origin policy satisfied (same port 80)
- ✅ CSP policy allows requests (`default-src 'self'`)
- ✅ Backend never directly exposed to frontend
- ✅ Network isolation maintained
- ✅ Scales to multiple backends

**Frontend Code - CORRECT:**
```typescript
// ✅ Relative path - uses same-origin
const API_BASE_URL = '/api'; // Resolved to http://localhost/api
```

**Frontend Code - INCORRECT:**
```typescript
// ❌ Hardcoded backend port - violates same-origin
const API_BASE_URL = 'http://localhost:5001/api'; // CSP violation!
```

## Best Practices

### ✅ DO

1. **Use services instead of direct httpClient calls**
   ```typescript
   // ✅ Good
   const data = await authService.userLogin(credentials);
   
   // ❌ Bad
   const data = await httpClient.fetch({ url: '/auth/login', ... });
   ```

2. **Handle errors properly**
   ```typescript
   // ✅ Good
   try {
     const data = await authService.userLogin(credentials);
   } catch (error) {
     const msg = getUserErrorMessage(error);
   }
   ```

3. **Use type-safe endpoint access**
   ```typescript
   // ✅ Good
   const url = API_CONFIG.endpoints.auth.login;
   
   // ❌ Bad
   const url = '/auth/login'; // Magic string
   ```

4. **Check authentication status**
   ```typescript
   // ✅ Good
   if (authService.isUserAuthenticated()) { ... }
   
   // ❌ Bad
   if (localStorage.getItem('auth_token')) { ... }
   ```

### ❌ DON'T

1. Don't bypass the HTTP client
2. Don't hardcode API URLs in components
3. Don't store sensitive data in localStorage
4. Don't ignore error responses
5. Don't manually manage authorization headers
6. Don't make direct fetch calls outside services

## Debugging Guide

### View All Requests (Development)
```javascript
// In browser console
httpClient.getRequestHistory()
```

### Download Request Log
```javascript
// In browser console
httpClient.downloadRequestHistory()
// Downloads: api-requests-2026-04-20T10:30:45.123Z.json
```

### Enable Logging
```javascript
// In .env
REACT_APP_LOG_REQUESTS=true
```

### Monitor Real-Time
```javascript
// Open browser DevTools → Application → Local Storage
// See: auth_token, admin_auth_token

// Open browser DevTools → Network tab
// All requests show with:
// - Method (GET, POST, etc.)
// - URL (/api/...)
// - Status code
// - Response time
```

### Check Authentication State
```javascript
// In browser console
authService.isUserAuthenticated() // true/false
authService.getUserToken() // token string or null
authService.isAdminAuthenticated() // true/false
authService.getAdminToken() // token string or null
```

## Performance Considerations

### Request Timeout
- **Default:** 30,000ms (30 seconds)
- **Configurable:** `REACT_APP_API_TIMEOUT`

### Retry Strategy
- **Type:** Exponential backoff
- **Max Retries:** 3
- **Delays:** 1s → 2s → 4s
- **Applicable:** 408, 429, 500, 502, 503, 504

### Caching
- **Currently:** No caching (simple first)
- **Future:** Add React Query or SWR for smart caching

## Security Considerations

### Authentication
- ✅ Tokens stored in localStorage
- ✅ Tokens sent in Authorization header
- ✅ Automatic logout on 401
- ✅ Token cleared on logout

### Headers
- ✅ CSP: `default-src 'self'` (only same-origin allowed)
- ✅ HSTS: Forces HTTPS
- ✅ X-Frame-Options: Prevents clickjacking
- ✅ Content-Type: JSON enforced

### Network
- ✅ Backend on isolated network (backend-nginx)
- ✅ Frontend never directly accesses backend:5001
- ✅ All requests routed through Nginx reverse proxy
- ✅ SQL Server on isolated network (backend-sql)

## Future Enhancements

1. **Request Caching**
   - Implement React Query for smart caching
   - Cache GET requests, invalidate on mutations

2. **Request Queuing**
   - Queue requests when offline
   - Replay when online

3. **Request Deduplication**
   - Cancel duplicate in-flight requests
   - Merge responses

4. **Analytics Integration**
   - Track request metrics
   - Monitor API performance
   - Alert on errors

5. **Mock Interceptor**
   - Intercept requests for testing
   - Return mock data

## Support & Troubleshooting

### CSP Policy Violations
**Problem:** `Refused to connect because it violates the document's Content Security Policy`

**Solution:** Ensure all API calls use `/api/*` relative path, not hardcoded `http://localhost:5001`

### CORS Errors
**Problem:** `Cross-Origin Request Blocked`

**Solution:** All requests must go through `/api/*` path (Nginx routes them internally)

### 401 Unauthorized
**Problem:** Requests returning 401

**Solution:** Check if token is still valid, may need to re-login

### Network Errors
**Problem:** `Failed to fetch`

**Solution:** Check browser console, verify backend is running

---

**Version:** 1.0  
**Last Updated:** April 20, 2026  
**Status:** Production Ready ✅
