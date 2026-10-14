import { EntitlementsProvider, useEntitlements } from '@/lib/entitlements';
import { apiClient } from '@/lib/api';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('@/lib/api', () => ({
  apiClient: {
    getEntitlements: vi.fn(),
    createCheckoutSession: vi.fn(),
  },
}));

function CheckoutButtons() {
  const { startCheckout } = useEntitlements();
  return (
    <>
      <button type="button" onClick={() => void startCheckout('operator', '/pricing')}>
        Choose Operator
      </button>
      <button type="button" onClick={() => void startCheckout('commander', '/pricing')}>
        Choose Commander
      </button>
    </>
  );
}

describe('entitlements checkout plan', () => {
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
    vi.mocked(apiClient.createCheckoutSession).mockResolvedValue({ url: 'https://checkout.test/session' });
  });

  it('creates the session for the plan the caller chose', async () => {
    render(
      <EntitlementsProvider>
        <CheckoutButtons />
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
