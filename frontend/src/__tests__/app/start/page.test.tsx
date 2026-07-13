import MapPage from '@/app/map/page';
import OnboardingPage from '@/app/onboarding/page';
import StartPage from '@/app/start/page';
import { render, screen } from '@testing-library/react';
import { redirect } from 'next/navigation';
import { beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('next/navigation', () => ({
  redirect: vi.fn(),
}));

vi.mock('@/components/marketing/MarketingNav', () => ({
  MarketingNav: () => <div>Marketing nav</div>,
}));

vi.mock('@/components/marketing/MarketingFooter', () => ({
  MarketingFooter: () => <div>Marketing footer</div>,
}));

vi.mock('@/components/marketing/OnboardingExperience', () => ({
  OnboardingExperience: ({ mode }: { mode: string }) => <div>Onboarding mode: {mode}</div>,
}));

describe('/start canonical onboarding route', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('resolves mode="new" when no searchParams are provided', async () => {
    render(await StartPage({}));

    expect(screen.getByText('Onboarding mode: new')).toBeInTheDocument();
  });

  it('resolves mode="new" when searchParams is undefined', async () => {
    render(await StartPage({ searchParams: undefined }));

    expect(screen.getByText('Onboarding mode: new')).toBeInTheDocument();
  });

  it('resolves mode="existing" for ?mode=existing', async () => {
    render(await StartPage({ searchParams: Promise.resolve({ mode: 'existing' }) }));

    expect(screen.getByText('Onboarding mode: existing')).toBeInTheDocument();
  });

  it('falls back to mode="new" for an unrecognized mode value', async () => {
    render(await StartPage({ searchParams: Promise.resolve({ mode: 'anything-else' }) }));

    expect(screen.getByText('Onboarding mode: new')).toBeInTheDocument();
  });
});

describe('/map redirect', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('redirects to the canonical analyzer', () => {
    MapPage();

    expect(redirect).toHaveBeenCalledWith('/tools/analyzer');
    expect(redirect).toHaveBeenCalledTimes(1);
  });
});

describe('/onboarding redirect', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('redirects to /start when no mode is provided', async () => {
    await OnboardingPage({});

    expect(redirect).toHaveBeenCalledWith('/start');
    expect(redirect).toHaveBeenCalledTimes(1);
  });

  it('redirects to /start when searchParams is undefined', async () => {
    await OnboardingPage({ searchParams: undefined });

    expect(redirect).toHaveBeenCalledWith('/start');
  });

  it('redirects to /start when legacy mode is provided', async () => {
    await OnboardingPage({ searchParams: Promise.resolve({ mode: 'existing' }) });

    expect(redirect).toHaveBeenCalledWith('/start');
  });

  it('redirects to /start (not preserving mode) for an unrecognized mode value', async () => {
    await OnboardingPage({ searchParams: Promise.resolve({ mode: 'anything-else' }) });

    expect(redirect).toHaveBeenCalledWith('/start');
  });
});
