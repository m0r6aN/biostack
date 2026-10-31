import { apiClient } from '@/lib/api';
import { AuthProvider } from '@/lib/AuthProvider';
import { EntitlementsProvider, FREE_ENTITLEMENTS, useEntitlements } from '@/lib/entitlements';
import { render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

// A 401 from /api/v1/billing/entitlements is printed to the browser console on every public page for
// anonymous visitors. Anonymous visitors are on the free plan, so the call must not be made at all.

vi.mock('@/lib/api', () => ({
  apiClient: { getEntitlements: vi.fn(), createCheckoutSession: vi.fn() },
}));

const fetchMock = vi.fn();
vi.stubGlobal('fetch', fetchMock);

function Probe() {
  const { entitlements, loading } = useEntitlements();
  return <p>{loading ? 'loading' : `plan:${entitlements.plan}`}</p>;
}

const proEntitlements = { ...FREE_ENTITLEMENTS, isPro: true, plan: 'pro' as const };

describe('EntitlementsProvider auth gating', () => {
  beforeEach(() => {
    fetchMock.mockReset();
    vi.mocked(apiClient.getEntitlements).mockReset().mockResolvedValue(proEntitlements);
  });

  it('does not call the entitlements API for an anonymous visitor', async () => {
    fetchMock.mockResolvedValue({ ok: true, status: 200, json: async () => ({ authenticated: false, user: null }) });

    render(
      <AuthProvider>
        <EntitlementsProvider>
          <Probe />
        </EntitlementsProvider>
      </AuthProvider>,
    );

    expect(await screen.findByText('plan:free')).toBeInTheDocument();
    expect(apiClient.getEntitlements).not.toHaveBeenCalled();
  });

  it('does not call the entitlements API when the session check returns 401', async () => {
    fetchMock.mockResolvedValue({ ok: false, status: 401, json: async () => ({}) });

    render(
      <AuthProvider>
        <EntitlementsProvider>
          <Probe />
        </EntitlementsProvider>
      </AuthProvider>,
    );

    expect(await screen.findByText('plan:free')).toBeInTheDocument();
    expect(apiClient.getEntitlements).not.toHaveBeenCalled();
  });

  it('loads entitlements once a signed-in session is known', async () => {
    fetchMock.mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => ({ authenticated: true, user: { id: 'u1', email: 'a@example.test', displayName: 'A', role: 0 } }),
    });

    render(
      <AuthProvider>
        <EntitlementsProvider>
          <Probe />
        </EntitlementsProvider>
      </AuthProvider>,
    );

    await waitFor(() => expect(screen.getByText('plan:pro')).toBeInTheDocument());
    expect(apiClient.getEntitlements).toHaveBeenCalledTimes(1);
  });

  it('still loads entitlements when there is no AuthProvider (standalone use)', async () => {
    render(
      <EntitlementsProvider>
        <Probe />
      </EntitlementsProvider>,
    );

    await waitFor(() => expect(screen.getByText('plan:pro')).toBeInTheDocument());
    expect(apiClient.getEntitlements).toHaveBeenCalledTimes(1);
  });
});
