'use client';

import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import { getApiBaseUrl } from './apiBase';

export type AuthUser = {
  id: string;
  email: string;
  displayName: string;
  avatarUrl?: string | null;
  role: number;
};

const DEV_BYPASS_USER: AuthUser = {
  id: '00000000-0000-0000-0000-000000000001',
  email: 'dev@biostack.local',
  displayName: 'Dev Bypass User',
  avatarUrl: null,
  role: 1,
};

function isDevBypassAuthEnabled() {
  return process.env.NEXT_PUBLIC_DEV_BYPASS_AUTH === 'true';
}

type AuthContextValue = {
  user: AuthUser | null;
  loading: boolean;
  refresh: () => Promise<void>;
  logout: () => Promise<void>;
};

const API_URL = getApiBaseUrl();
const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [loading, setLoading] = useState(true);

  const refresh = useCallback(async () => {
    if (isDevBypassAuthEnabled()) {
      setUser(DEV_BYPASS_USER);
      setLoading(false);
      return;
    }

    try {
      const res = await fetch(`${API_URL}/api/v1/auth/session`, {
        credentials: 'include',
        cache: 'no-store',
      });
      if (!res.ok) {
        if (res.status >= 500) {
          console.warn('BioStack session check failed', { status: res.status });
        }
        setUser(null);
        return;
      }

      const session = (await res.json()) as { authenticated: boolean; user: AuthUser | null };
      setUser(session.authenticated ? session.user : null);
    } catch {
      setUser(null);
    } finally {
      setLoading(false);
    }
  }, []);

  const logout = useCallback(async () => {
    if (isDevBypassAuthEnabled()) {
      setUser(DEV_BYPASS_USER);
      window.location.href = '/';
      return;
    }

    await fetch(`${API_URL}/api/v1/auth/logout`, {
      method: 'POST',
      credentials: 'include',
    }).catch(() => undefined);
    setUser(null);
    window.location.href = '/auth/signin';
  }, []);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  const value = useMemo(() => ({ user, loading, refresh, logout }), [user, loading, refresh, logout]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within AuthProvider');
  }

  return context;
}
