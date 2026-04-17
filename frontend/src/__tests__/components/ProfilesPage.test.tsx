import ProfilesPage from '@/app/profiles/page';
import { apiClient } from '@/lib/api';
import { render, screen } from '@testing-library/react';
import type { ReactNode } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const setProfiles = vi.fn();
const setCurrentProfileId = vi.fn();

vi.mock('@/components/Header', () => ({
  Header: ({ title, actions }: { title: string; actions?: ReactNode }) => (
    <header>
      <h1>{title}</h1>
      {actions}
    </header>
  ),
}));

vi.mock('@/components/profiles/ProfileForm', () => ({
  ProfileForm: () => <div>Profile form</div>,
}));

vi.mock('@/components/LoadingState', () => ({
  LoadingSkeleton: () => <div>Loading profiles</div>,
}));

vi.mock('@/lib/api', () => ({
  apiClient: {
    getProfiles: vi.fn(),
    getProfileGoals: vi.fn(),
    createProfile: vi.fn(),
    createCompound: vi.fn(),
    setProfileGoals: vi.fn(),
    deleteProfile: vi.fn(),
  },
}));

vi.mock('@/lib/context', () => ({
  useProfile: () => ({
    profiles: [],
    setProfiles,
    setCurrentProfileId,
  }),
}));

vi.mock('@/lib/settings', () => ({
  useSettings: () => ({ settings: { weightUnit: 'metric' } }),
}));

vi.mock('@/lib/onboardingPreview', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@/lib/onboardingPreview')>();
  return {
    ...actual,
    readOnboardingPreview: vi.fn(() => ({
      compounds: ['BPC-157', 'NAD+'],
      goals: [],
    })),
    clearOnboardingPreview: vi.fn(),
  };
});

describe('ProfilesPage continuation state', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(apiClient.getProfiles).mockResolvedValue([]);
  });

  it('shows recovered inputs as continuation rather than a reset empty state', async () => {
    render(<ProfilesPage />);

    expect(await screen.findByText('Inputs recovered.')).toBeInTheDocument();
    expect(screen.getAllByText('Profile not yet instantiated.').length).toBeGreaterThan(0);
    expect(screen.getAllByText('Your list is ready to save.').length).toBeGreaterThan(0);
    expect(screen.getAllByRole('button', { name: 'Continue Profile Setup' }).length).toBeGreaterThan(0);
    expect(screen.queryByText('Ready To Continue')).not.toBeInTheDocument();
    expect(screen.queryByText('Name the profile. Keep the protocol.')).not.toBeInTheDocument();
    expect(screen.queryByText('Create your first profile to get started')).not.toBeInTheDocument();
  });
});
