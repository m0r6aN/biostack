import BillingPage from '@/app/billing/page';
import { apiClient } from '@/lib/api';
import type { CurrentSubscription } from '@/lib/types';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { StrictMode } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('@/lib/api', () => ({
  apiClient: {
    getCurrentSubscription: vi.fn(),
    createCheckoutSession: vi.fn(),
    createBillingPortalSession: vi.fn(),
  },
}));

vi.mock('@/components/Header', () => ({
  Header: ({ title }: { title: string }) => <h1>{title}</h1>,
}));

vi.mock('@/components/LoadingState', () => ({
  LoadingSkeleton: () => <div>Loading</div>,
}));

vi.mock('@/components/ErrorState', () => ({
  ErrorState: ({ message }: { message: string }) => <div>{message}</div>,
}));

const observerSubscription = {
  tier: 'Observer',
  status: 'None',
  productCode: 'observer',
  isPaid: false,
  cancelAtPeriodEnd: false,
  currentPeriodEndUtc: null,
  features: {},
  limits: { active_compounds: 8 },
} satisfies CurrentSubscription;

describe('billing plan intent', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    window.history.replaceState({}, '', '/billing');
    vi.mocked(apiClient.getCurrentSubscription).mockResolvedValue(
      observerSubscription,
    );
  });

  it('consumes a valid plan once under Strict Mode and does not repeat on remount', async () => {
    window.history.replaceState({}, '', '/billing?plan=operator');
    vi.mocked(apiClient.createCheckoutSession).mockImplementation(
      () => new Promise(() => undefined),
    );

    const first = render(
      <StrictMode>
        <BillingPage />
      </StrictMode>,
    );

    await waitFor(() => {
      expect(apiClient.createCheckoutSession).toHaveBeenCalledTimes(1);
    });
    expect(apiClient.createCheckoutSession).toHaveBeenCalledWith('operator');
    expect(window.location.search).toBe('');

    first.unmount();
    render(<BillingPage />);

    expect(await screen.findByText('Core tracking is active.')).toBeInTheDocument();
    expect(apiClient.createCheckoutSession).toHaveBeenCalledTimes(1);
  });

  it('preserves plan intent and creates no checkout when authentication is not confirmed', async () => {
    window.history.replaceState({}, '', '/billing?plan=commander');
    vi.mocked(apiClient.getCurrentSubscription).mockRejectedValue(
      new Error('unauthorized'),
    );

    render(<BillingPage />);

    expect(
      await screen.findByText('Billing state could not be loaded.'),
    ).toBeInTheDocument();
    expect(apiClient.createCheckoutSession).not.toHaveBeenCalled();
    expect(window.location.search).toBe('?plan=commander');
  });

  it('rejects an invalid plan without hiding the manual plan controls', async () => {
    window.history.replaceState({}, '', '/billing?plan=enterprise');

    render(<BillingPage />);

    expect(
      await screen.findByText(
        'That plan link is not valid. Choose an available plan below.',
      ),
    ).toBeInTheDocument();
    expect(apiClient.createCheckoutSession).not.toHaveBeenCalled();
    expect(
      screen.getByRole('button', { name: 'Upgrade to Operator' }),
    ).toBeEnabled();
  });

  it('reports checkout failure honestly and allows a manual retry', async () => {
    window.history.replaceState({}, '', '/billing?plan=operator');
    vi.mocked(apiClient.createCheckoutSession).mockRejectedValueOnce(
      new Error('not configured'),
    );

    render(<BillingPage />);

    expect(
      await screen.findByText(
        'Checkout could not be started. Choose a plan below to try again.',
      ),
    ).toBeInTheDocument();
    expect(window.location.search).toBe('');

    vi.mocked(apiClient.createCheckoutSession).mockImplementation(
      () => new Promise(() => undefined),
    );
    fireEvent.click(
      screen.getByRole('button', { name: 'Upgrade to Operator' }),
    );

    await waitFor(() => {
      expect(apiClient.createCheckoutSession).toHaveBeenCalledTimes(2);
    });
  });

  it('creates one session when the manual plan control is clicked rapidly', async () => {
    vi.mocked(apiClient.createCheckoutSession).mockImplementation(
      () => new Promise(() => undefined),
    );

    render(<BillingPage />);

    const operatorButton = await screen.findByRole('button', {
      name: 'Upgrade to Operator',
    });
    fireEvent.click(operatorButton);
    fireEvent.click(operatorButton);

    await waitFor(() => {
      expect(apiClient.createCheckoutSession).toHaveBeenCalledTimes(1);
    });
    expect(apiClient.createCheckoutSession).toHaveBeenCalledWith('operator');
  });
});
