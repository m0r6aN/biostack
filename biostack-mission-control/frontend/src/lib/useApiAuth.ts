'use client';

import { useSession } from 'next-auth/react';
import { useEffect } from 'react';
import { apiClient } from './api';

/**
 * Drop this hook inside any top-level client component (e.g. ProfileProvider)
 * to keep the ApiClient's access token in sync with the NextAuth session.
 */
export function useApiAuth() {
  const { data: session } = useSession();

  useEffect(() => {
    apiClient.setAccessToken(session?.backendAccessToken ?? null);
  }, [session?.backendAccessToken]);
}
