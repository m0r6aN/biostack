import PricingClient from '@/app/pricing/pricing-client';
import { apiClient } from '@/lib/api';
import { EntitlementsProvider } from '@/lib/entitlements';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('@/lib/api', () => ({
  apiClient: {
    getEntitlements: vi.fn(),
    getCurrentSubscription: vi.fn(),
    createCheckoutSession: vi.fn(),
    captureLead: vi.fn(),
  },
}));

vi.mock('@/components/marketing/MarketingNav', () => ({
  MarketingNav: () => <div>Marketing nav</div>,
}));

vi.mock('@/components/marketing/MarketingFooter', () => ({
  MarketingFooter: () => <div>Marketing footer</div>,
}));

describe('pricing checkout plan', () => {
  beforeEach(() => {
    vi.mocked(apiClient.getEntitlements).mockResolvedValue({
      isPro: false,
      plan: 'free',
      limits: { maxCompounds: 8 },
      features: {
        stackIntelligence: false,
        fullOverlapAnalysis: false,
        protocolBuilder: false,
        observabilityCorrelations: false,
        savedAdvancedProtocolViews: false,
        affiliateSurfaces: true,
      },
      futureRoles: [],
    });
    vi.mocked(apiClient.getCurrentSubscription).mockResolvedValue({
      tier: 'Observer',
      status: 'None',
      productCode: 'observer',
      isPaid: false,
      cancelAtPeriodEnd: false,
      currentPeriodEndUtc: null,
      features: {},
      limits: { active_compounds: 8 },
    });
    vi.mocked(apiClient.createCheckoutSession).mockResolvedValue({ url: 'https://checkout.test/session' });
  });

  it('starts Operator and Commander checkout for the selected plan', async () => {
    render(
      <EntitlementsProvider>
        <PricingClient />
      </EntitlementsProvider>,
    );

    fireEvent.click(await screen.findByRole('button', { name: 'Choose Operator' }));
    fireEvent.click(screen.getByRole('button', { name: 'Choose Commander' }));

    await waitFor(() => {
      expect(apiClient.createCheckoutSession).toHaveBeenNthCalledWith(1, 'operator');
      expect(apiClient.createCheckoutSession).toHaveBeenNthCalledWith(2, 'commander');
    });
  });
});
