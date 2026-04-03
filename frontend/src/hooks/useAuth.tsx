import React, { createContext, useContext, useMemo, useState } from 'react';
import { authService } from '../services/AuthService';
import { ApiResponse, AuthResponse, LoginRequest, RegisterRequest } from '../types';

interface AuthContextState {
  token: string | null;
  isAuthenticated: boolean;
  login: (payload: LoginRequest) => Promise<ApiResponse<AuthResponse>>;
  register: (payload: RegisterRequest) => Promise<ApiResponse<AuthResponse>>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextState | undefined>(undefined);
const STORAGE_KEY = 'trading-platform-auth-token';

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [token, setToken] = useState<string | null>(() => {
    return localStorage.getItem(STORAGE_KEY);
  });

  const login = async (payload: LoginRequest): Promise<ApiResponse<AuthResponse>> => {
    const result = await authService.login(payload);
    if (!result.error && result.data?.token) {
      localStorage.setItem(STORAGE_KEY, result.data.token);
      setToken(result.data.token);
    }
    return result;
  };

  const register = async (payload: RegisterRequest): Promise<ApiResponse<AuthResponse>> => {
    return authService.register(payload);
  };

  const logout = (): void => {
    localStorage.removeItem(STORAGE_KEY);
    setToken(null);
  };

  const value = useMemo(
    () => ({
      token,
      isAuthenticated: Boolean(token),
      login,
      register,
      logout,
    }),
    [token]
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
