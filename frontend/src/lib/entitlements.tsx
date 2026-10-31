'use client';

import { apiClient } from '@/lib/api';
import { useOptionalAuth } from '@/lib/AuthProvider';
import { Entitlements } from '@/lib/types';
import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';

export const FREE_ENTITLEMENTS: Entitlements = {
  isPro: false,
  plan: 'free',
  limits: { maxCompounds: 2 },
  features: {
    stackIntelligence: false,
    fullOverlapAnalysis: false,
    protocolBuilder: false,
    observabilityCorrelations: false,
    savedAdvancedProtocolViews: false,
  },
  futureRoles: ['retail_admin', 'client_seat', 'white_label'],
};

export const PRO_ENTITLEMENTS: Entitlements = {
  isPro: true,
  plan: 'pro',
  limits: { maxCompounds: Number.MAX_SAFE_INTEGER },
  features: {
    stackIntelligence: true,
    fullOverlapAnalysis: true,
    protocolBuilder: true,
    observabilityCorrelations: true,
    savedAdvancedProtocolViews: true,
  },
  futureRoles: FREE_ENTITLEMENTS.futureRoles,
};

type EntitlementsContextValue = {
  entitlements: Entitlements;
  loading: boolean;
  refreshEntitlements: () => Promise<Entitlements>;
  startCheckout: (plan: 'operator' | 'commander', returnPath?: string) => Promise<void>;
};

const EntitlementsContext = createContext<EntitlementsContextValue | undefined>(undefined);

export function EntitlementsProvider({ children }: { children: React.ReactNode }) {
  const [entitlements, setEntitlements] = useState<Entitlements>(FREE_ENTITLEMENTS);
  const [loading, setLoading] = useState(true);
  const auth = useOptionalAuth();
  const authLoading = auth ? auth.loading : false;
  const signedIn = auth ? Boolean(auth.user) : true;

  const refreshEntitlements = useCallback(async () => {
    try {
      const next = await apiClient.getEntitlements();
      setEntitlements(next);
      return next;
    } catch {
      setEntitlements(FREE_ENTITLEMENTS);
      return FREE_ENTITLEMENTS;
    } finally {
      setLoading(false);
    }
  }, []);

  const startCheckout = useCallback(async (plan: 'operator' | 'commander') => {
    const { url: checkoutUrl } = await apiClient.createCheckoutSession(plan);
    window.location.href = checkoutUrl;
  }, []);

  // Anonymous visitors are on the free plan by definition. Asking the API anyway returns a 401
  // that the browser prints to the console on every public page, so skip the call until a
  // signed-in session is known.
  const anonymous = !authLoading && !signedIn;

  useEffect(() => {
    if (authLoading || !signedIn) return;
    void refreshEntitlements();
  }, [authLoading, signedIn, refreshEntitlements]);

  const value = useMemo(
    () => ({
      entitlements: anonymous ? FREE_ENTITLEMENTS : entitlements,
      loading: anonymous ? false : loading,
      refreshEntitlements,
      startCheckout,
    }),
    [anonymous, entitlements, loading, refreshEntitlements, startCheckout]
  );

  return <EntitlementsContext.Provider value={value}>{children}</EntitlementsContext.Provider>;
}

export function useEntitlements() {
  const context = useContext(EntitlementsContext);
  if (!context) {
    return {
      entitlements: PRO_ENTITLEMENTS,
      loading: false,
      refreshEntitlements: async () => PRO_ENTITLEMENTS,
      startCheckout: async () => {
        throw new Error('EntitlementsProvider is required to start checkout.');
      },
    };
  }
  return context;
}
