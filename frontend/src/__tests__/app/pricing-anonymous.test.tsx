import PricingClient from '@/app/pricing/pricing-client';
import { apiClient } from '@/lib/api';
import { AuthProvider } from '@/lib/AuthProvider';
import { EntitlementsProvider } from '@/lib/entitlements';
import { render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('@/lib/api', () => ({
  apiClient: { getEntitlements: vi.fn(), getCurrentSubscription: vi.fn(), createCheckoutSession: vi.fn(), captureLead: vi.fn() },
}));
vi.mock('@/components/marketing/MarketingNav', () => ({ MarketingNav: () => <div>Marketing nav</div> }));
vi.mock('@/components/marketing/MarketingFooter', () => ({ MarketingFooter: () => <div>Marketing footer</div> }));

const fetchMock = vi.fn();
vi.stubGlobal('fetch', fetchMock);

describe('pricing page for an anonymous visitor', () => {
  beforeEach(() => {
    fetchMock.mockReset().mockResolvedValue({ ok: true, status: 200, json: async () => ({ authenticated: false, user: null }) });
    vi.mocked(apiClient.getEntitlements).mockReset();
    vi.mocked(apiClient.getCurrentSubscription).mockReset();
  });

  it('does not request the subscription or entitlements (each would log a 401 in the console)', async () => {
    render(
      <AuthProvider>
        <EntitlementsProvider>
          <PricingClient />
        </EntitlementsProvider>
      </AuthProvider>,
    );

    await waitFor(() => expect(fetchMock).toHaveBeenCalled());
    await screen.findByRole('button', { name: 'Choose Operator' });
    await waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(1));
    expect(apiClient.getCurrentSubscription).not.toHaveBeenCalled();
    expect(apiClient.getEntitlements).not.toHaveBeenCalled();
  });
});
