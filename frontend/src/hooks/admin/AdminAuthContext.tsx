import React, { createContext, useContext, useState, useEffect, useMemo, useCallback } from 'react';

/**
 * ADMIN AUTH CONTEXT STATE - Singular Source of Truth
 * 
 * This is the "brain" of admin authentication system.
 * Every admin page accesses state through this interface.
 * 
 * State Lifecycle:
 * 1. BOOTSTRAP/REGISTER: temp token (5 min)
 * 2. LOGIN: temp token + sessionId (5 min)
 * 3. 2FA VERIFY: final JWT (60 min)
 * 4. DASHBOARD: protected by token + expiry check
 */
export interface AdminAuthContextState {
  // ===== CORE STATE =====
  token: string | null;              // JWT (temp or final)
  sessionId: string | null;           // Backend sessionId (for 2FA step)
  adminId: string | null;             // Admin UUID
  username: string | null;            // Admin username
  email: string | null;               // Admin email (optional)

  // ===== FLOW FLAGS =====
  isTempToken: boolean;               // Is this 5-min temp token?
  requiresTwoFactor: boolean;          // Does backend require 2FA?
  isAuthenticated: boolean;            // Has final JWT? (isTempToken === false)

  // ===== SESSION MANAGEMENT =====
  setSession: (sessionData: AdminSessionData) => void;
  clearSession: () => void;
  logout: () => void;

  // ===== TOKEN VALIDATION =====
  isTokenExpired: () => boolean;
  getTokenExpiry: () => number | null;  // Unix timestamp (seconds)

  // ===== STORAGE KEY =====
  readonly STORAGE_KEY: string;
}

/**
 * DATA PASSED TO setSession()
 * Single object with all session info
 */
export interface AdminSessionData {
  token: string;
  sessionId?: string;
  adminId?: string;
  username?: string;
  email?: string;
  isTempToken?: boolean;
  requiresTwoFactor?: boolean;
}

// ===== CONTEXT CREATION =====
const AdminAuthContext = createContext<AdminAuthContextState | undefined>(undefined);

// Storage key (match User auth pattern)
const STORAGE_KEY = 'trading-admin-session';

/**
 * ADMIN AUTH PROVIDER
 * 
 * Wraps admin section (or whole app)
 * Manages session state + persistence
 * Syncs with localStorage
 */
export const AdminAuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  // ===== STATE =====
  const [token, setTokenState] = useState<string | null>(() => {
    try {
      const stored = localStorage.getItem(STORAGE_KEY);
      if (!stored) return null;
      const parsed = JSON.parse(stored);
      return parsed.token || null;
    } catch {
      return null;
    }
  });

  const [sessionId, setSessionIdState] = useState<string | null>(() => {
    try {
      const stored = localStorage.getItem(STORAGE_KEY);
      if (!stored) return null;
      const parsed = JSON.parse(stored);
      return parsed.sessionId || null;
    } catch {
      return null;
    }
  });

  const [adminId, setAdminIdState] = useState<string | null>(() => {
    try {
      const stored = localStorage.getItem(STORAGE_KEY);
      if (!stored) return null;
      const parsed = JSON.parse(stored);
      return parsed.adminId || null;
    } catch {
      return null;
    }
  });

  const [username, setUsernameState] = useState<string | null>(() => {
    try {
      const stored = localStorage.getItem(STORAGE_KEY);
      if (!stored) return null;
      const parsed = JSON.parse(stored);
      return parsed.username || null;
    } catch {
      return null;
    }
  });

  const [email, setEmailState] = useState<string | null>(() => {
    try {
      const stored = localStorage.getItem(STORAGE_KEY);
      if (!stored) return null;
      const parsed = JSON.parse(stored);
      return parsed.email || null;
    } catch {
      return null;
    }
  });

  const [isTempToken, setIsTempTokenState] = useState<boolean>(() => {
    try {
      const stored = localStorage.getItem(STORAGE_KEY);
      if (!stored) return false;
      const parsed = JSON.parse(stored);
      return parsed.isTempToken === true;
    } catch {
      return false;
    }
  });

  const [requiresTwoFactor, setRequiresTwoFactorState] = useState<boolean>(() => {
    try {
      const stored = localStorage.getItem(STORAGE_KEY);
      if (!stored) return false;
      const parsed = JSON.parse(stored);
      return parsed.requiresTwoFactor === true;
    } catch {
      return false;
    }
  });

  // ===== HELPER: Decode JWT payload =====
  const decodeJwtPayload = useCallback((jwt: string): any => {
    try {
      const parts = jwt.split('.');
      if (parts.length !== 3) return null;
      const decoded = JSON.parse(atob(parts[1]));
      return decoded;
    } catch {
      return null;
    }
  }, []);

  // ===== HELPER: Check if token expired =====
  const isTokenExpired = useCallback((): boolean => {
    if (!token) return true;

    const payload = decodeJwtPayload(token);
    if (!payload || !payload.exp) return true;

    const expirySeconds = payload.exp;
    const nowSeconds = Math.floor(Date.now() / 1000);
    return nowSeconds >= expirySeconds;
  }, [token, decodeJwtPayload]);

  // ===== HELPER: Get token expiry as Unix timestamp =====
  const getTokenExpiry = useCallback((): number | null => {
    if (!token) return null;

    const payload = decodeJwtPayload(token);
    if (!payload || !payload.exp) return null;

    return payload.exp;
  }, [token, decodeJwtPayload]);

  // ===== AUTO-LOGOUT ON TOKEN EXPIRY =====
  useEffect(() => {
    if (!token || isTokenExpired()) {
      return; // Not logged in or already expired
    }

    const payload = decodeJwtPayload(token);
    if (!payload?.exp) return;

    const expirySeconds = payload.exp;
    const nowSeconds = Math.floor(Date.now() / 1000);
    const timeUntilExpiry = (expirySeconds - nowSeconds) * 1000;

    if (timeUntilExpiry <= 0) {
      // Already expired
      return;
    }

    // Set timeout to auto-logout 30 seconds before expiry
    const logoutBuffer = 30 * 1000; // 30 seconds
    const timeoutMs = Math.max(timeUntilExpiry - logoutBuffer, 1000);

    console.log(`[AdminAuth] Token expires in ${Math.floor(timeUntilExpiry / 1000)}s. Auto-logout in ${Math.floor(timeoutMs / 1000)}s`);

    const timeout = setTimeout(() => {
      console.log('[AdminAuth] Token expired - auto-logout');
      // We'll let page component handle redirect
    }, timeoutMs);

    return () => clearTimeout(timeout);
  }, [token, isTokenExpired, decodeJwtPayload]);

  // ===== CALLBACK: Save session to state + localStorage =====
  const setSession = useCallback((sessionData: AdminSessionData) => {
    const newSessionState = {
      token: sessionData.token,
      sessionId: sessionData.sessionId ?? sessionId,
      adminId: sessionData.adminId ?? adminId,
      username: sessionData.username ?? username,
      email: sessionData.email ?? email,
      isTempToken: sessionData.isTempToken ?? isTempToken,
      requiresTwoFactor: sessionData.requiresTwoFactor ?? requiresTwoFactor,
    };

    // Update state
    setTokenState(sessionData.token);
    setSessionIdState(newSessionState.sessionId);
    setAdminIdState(newSessionState.adminId);
    setUsernameState(newSessionState.username);
    setEmailState(newSessionState.email);
    setIsTempTokenState(newSessionState.isTempToken);
    setRequiresTwoFactorState(newSessionState.requiresTwoFactor);

    // Persist to localStorage
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(newSessionState));
      console.log('[AdminAuth] Session saved:', {
        token: '***',
        sessionId: newSessionState.sessionId?.substring(0, 8),
        username: newSessionState.username,
        isTempToken: newSessionState.isTempToken,
      });
    } catch (error) {
      console.error('[AdminAuth] Failed to persist session:', error);
    }
  }, [sessionId, adminId, username, email, isTempToken, requiresTwoFactor]);

  // ===== CALLBACK: Clear session from state + localStorage =====
  const clearSession = useCallback(() => {
    setTokenState(null);
    setSessionIdState(null);
    setAdminIdState(null);
    setUsernameState(null);
    setEmailState(null);
    setIsTempTokenState(false);
    setRequiresTwoFactorState(false);

    try {
      localStorage.removeItem(STORAGE_KEY);
      console.log('[AdminAuth] Session cleared');
    } catch (error) {
      console.error('[AdminAuth] Failed to clear session:', error);
    }
  }, []);

  // ===== CALLBACK: Logout (alias for clearSession) =====
  const logout = useCallback(() => {
    clearSession();
  }, [clearSession]);

  // ===== CONTEXT VALUE =====
  const value = useMemo<AdminAuthContextState>(
    () => ({
      token,
      sessionId,
      adminId,
      username,
      email,
      isTempToken,
      requiresTwoFactor,
      isAuthenticated: token !== null && !isTempToken && !isTokenExpired(),
      setSession,
      clearSession,
      logout,
      isTokenExpired,
      getTokenExpiry,
      STORAGE_KEY,
    }),
    [token, sessionId, adminId, username, email, isTempToken, requiresTwoFactor, setSession, clearSession, logout, isTokenExpired, getTokenExpiry]
  );

  return <AdminAuthContext.Provider value={value}>{children}</AdminAuthContext.Provider>;
};

/**
 * HOOK: Access admin auth context
 * 
 * Usage:
 * const { token, isAuthenticated, setSession, clearSession } = useAdminAuth();
 */
export const useAdminAuth = (): AdminAuthContextState => {
  const context = useContext(AdminAuthContext);

  if (!context) {
    throw new Error('useAdminAuth must be used within AdminAuthProvider');
  }

  return context;
};
