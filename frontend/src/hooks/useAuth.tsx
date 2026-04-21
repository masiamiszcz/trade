import React, { createContext, useContext, useMemo, useState, useEffect } from 'react';
import { authService } from '../services/AuthenticationService';
import { ApiResponse, AuthResponse, LoginRequest, RegisterRequest } from '../types';

interface AuthContextState {
  token: string | null;
  isAuthenticated: boolean;
  login: (payload: LoginRequest) => Promise<ApiResponse<AuthResponse>>;
  register: (payload: RegisterRequest) => Promise<ApiResponse<AuthResponse>>;
  logout: () => void;

  // 2FA Session State (for temp tokens)
  tempToken: string | null;
  sessionId: string | null;
  requires2FA: boolean;
  setTempSession: (token: string, sessionId: string) => void;
  clearTempSession: () => void;
}

const AuthContext = createContext<AuthContextState | undefined>(undefined);
const STORAGE_KEY = 'auth_token'; // ✅ Match AuthenticationService.ts storage key!
const TEMP_STORAGE_KEY = 'trading-platform-temp-token';
const SESSION_STORAGE_KEY = 'trading-platform-session-id';

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [token, setToken] = useState<string | null>(() => {
    return localStorage.getItem(STORAGE_KEY);
  });

  const [tempToken, setTempTokenState] = useState<string | null>(() => {
    return localStorage.getItem(TEMP_STORAGE_KEY);
  });

  const [sessionId, setSessionIdState] = useState<string | null>(() => {
    return localStorage.getItem(SESSION_STORAGE_KEY);
  });

  const [requires2FA, setRequires2FA] = useState<boolean>(false);

  // ✅ Sync with AuthenticationService.setUserToken() via custom event
  // Using custom event instead of storage events because storage events 
  // DON'T fire in the same tab where the write happens!
  useEffect(() => {
    const handleTokenUpdate = (e: Event) => {
      const customEvent = e as CustomEvent;
      const newToken = customEvent.detail?.token || null;
      setToken(newToken);
      console.log('[useAuth] 🔄 Token updated via userTokenUpdated event:', !!newToken);
    };

    window.addEventListener('userTokenUpdated', handleTokenUpdate);
    return () => window.removeEventListener('userTokenUpdated', handleTokenUpdate);
  }, []);

  const login = async (payload: LoginRequest): Promise<ApiResponse<AuthResponse>> => {
    try {
      const result = await authService.userLogin(payload);
      if (result.token) {
        localStorage.setItem(STORAGE_KEY, result.token);
        setToken(result.token);
      }
      return { data: result };
    } catch (error) {
      return {
        error: {
          message: error instanceof Error ? error.message : 'Login failed',
          status: error instanceof Error && 'status' in error ? (error as any).status : undefined,
        },
      };
    }
  };

  const register = async (payload: RegisterRequest): Promise<ApiResponse<AuthResponse>> => {
    try {
      const result = await authService.userRegister(payload);
      if (result.token) {
        localStorage.setItem(STORAGE_KEY, result.token);
        setToken(result.token);
      }
      return { data: result };
    } catch (error) {
      return {
        error: {
          message: error instanceof Error ? error.message : 'Registration failed',
          status: error instanceof Error && 'status' in error ? (error as any).status : undefined,
        },
      };
    }
  };

  const logout = (): void => {
    localStorage.removeItem(STORAGE_KEY);
    localStorage.removeItem(TEMP_STORAGE_KEY);
    localStorage.removeItem(SESSION_STORAGE_KEY);
    setToken(null);
    setTempTokenState(null);
    setSessionIdState(null);
    setRequires2FA(false);
  };

  const setTempSession = (newTempToken: string, newSessionId: string): void => {
    localStorage.setItem(TEMP_STORAGE_KEY, newTempToken);
    localStorage.setItem(SESSION_STORAGE_KEY, newSessionId);
    setTempTokenState(newTempToken);
    setSessionIdState(newSessionId);
    setRequires2FA(true);
  };

  const clearTempSession = (): void => {
    localStorage.removeItem(TEMP_STORAGE_KEY);
    localStorage.removeItem(SESSION_STORAGE_KEY);
    setTempTokenState(null);
    setSessionIdState(null);
    setRequires2FA(false);
  };

  const value = useMemo(
    () => ({
      token,
      isAuthenticated: Boolean(token),
      login,
      register,
      logout,
      tempToken,
      sessionId,
      requires2FA,
      setTempSession,
      clearTempSession,
    }),
    [token, tempToken, sessionId, requires2FA]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};

export const useAuth = (): AuthContextState => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within AuthProvider');
  }
  return context;
};
