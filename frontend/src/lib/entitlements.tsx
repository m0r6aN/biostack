'use client';

import { apiClient } from '@/lib/api';
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
    affiliateSurfaces: true,
  },
  futureRoles: ['retail_admin', 'client_seat', 'white_label', 'affiliate_enabled'],
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
    affiliateSurfaces: true,
  },
  futureRoles: FREE_ENTITLEMENTS.futureRoles,
};

type EntitlementsContextValue = {
  entitlements: Entitlements;
  loading: boolean;
  refreshEntitlements: () => Promise<Entitlements>;
  startCheckout: (returnPath?: string) => Promise<void>;
};

const EntitlementsContext = createContext<EntitlementsContextValue | undefined>(undefined);

export function EntitlementsProvider({ children }: { children: React.ReactNode }) {
  const [entitlements, setEntitlements] = useState<Entitlements>(FREE_ENTITLEMENTS);
  const [loading, setLoading] = useState(true);

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

  const startCheckout = useCallback(async (returnPath?: string) => {
    const { url: checkoutUrl } = await apiClient.createCheckoutSession('operator');
    window.location.href = checkoutUrl;
  }, []);

  useEffect(() => {
    void refreshEntitlements();
  }, [refreshEntitlements]);

  const value = useMemo(
    () => ({ entitlements, loading, refreshEntitlements, startCheckout }),
    [entitlements, loading, refreshEntitlements, startCheckout]
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
