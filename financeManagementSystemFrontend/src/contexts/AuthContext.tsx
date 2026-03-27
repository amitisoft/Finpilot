import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import { ApiClient } from '../lib/api';
import type { AuthResponse, CurrentUserResponse } from '../types/api';

interface AuthContextValue {
  token: string | null;
  refreshToken: string | null;
  user: CurrentUserResponse | null;
  loading: boolean;
  api: ApiClient;
  login: (payload: AuthResponse) => Promise<void>;
  logout: () => Promise<void>;
}

const STORAGE_KEY = 'finpilot-auth';
const AuthContext = createContext<AuthContextValue | undefined>(undefined);

function readStoredAuth() {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    return raw ? (JSON.parse(raw) as { token: string; refreshToken: string }) : null;
  } catch {
    return null;
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [token, setToken] = useState<string | null>(() => readStoredAuth()?.token ?? null);
  const [refreshToken, setRefreshToken] = useState<string | null>(() => readStoredAuth()?.refreshToken ?? null);
  const [user, setUser] = useState<CurrentUserResponse | null>(null);
  const [loading, setLoading] = useState(true);

  const api = useMemo(() => new ApiClient(() => token), [token]);

  const persist = useCallback((nextToken: string | null, nextRefreshToken: string | null) => {
    if (!nextToken || !nextRefreshToken) {
      localStorage.removeItem(STORAGE_KEY);
      return;
    }

    localStorage.setItem(STORAGE_KEY, JSON.stringify({ token: nextToken, refreshToken: nextRefreshToken }));
  }, []);

  const bootstrap = useCallback(async () => {
    if (!token) {
      setUser(null);
      setLoading(false);
      return;
    }

    try {
      const me = await api.get<CurrentUserResponse>('/api/Auth/me');
      setUser(me);
    } catch {
      setToken(null);
      setRefreshToken(null);
      setUser(null);
      persist(null, null);
    } finally {
      setLoading(false);
    }
  }, [api, persist, token]);

  useEffect(() => {
    void bootstrap();
  }, [bootstrap]);

  const login = useCallback(async (payload: AuthResponse) => {
    setToken(payload.accessToken);
    setRefreshToken(payload.refreshToken);
    persist(payload.accessToken, payload.refreshToken);

    const nextApi = new ApiClient(() => payload.accessToken);
    const me = await nextApi.get<CurrentUserResponse>('/api/Auth/me');
    setUser(me);
  }, [persist]);

  const logout = useCallback(async () => {
    const currentRefresh = refreshToken;
    try {
      if (currentRefresh) {
        await api.post('/api/Auth/logout', { refreshToken: currentRefresh });
      }
    } catch {
      // noop for local logout
    } finally {
      setToken(null);
      setRefreshToken(null);
      setUser(null);
      persist(null, null);
    }
  }, [api, persist, refreshToken]);

  const value = useMemo<AuthContextValue>(() => ({
    token,
    refreshToken,
    user,
    loading,
    api,
    login,
    logout
  }), [token, refreshToken, user, loading, api, login, logout]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used inside AuthProvider');
  }

  return context;
}
