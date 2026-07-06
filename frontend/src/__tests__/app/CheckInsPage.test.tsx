import CheckInsPage from '@/app/checkins/page';
import { render, screen, waitFor } from '@testing-library/react';
import type { ReactNode } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const useProfileMock = vi.fn();

vi.mock('@/lib/context', () => ({
  useProfile: () => useProfileMock(),
}));

vi.mock('next/navigation', () => ({
  useRouter: () => ({ push: vi.fn() }),
}));

vi.mock('@/lib/settings', () => ({
  useSettings: () => ({ settings: { weightUnit: 'metric' } }),
}));

vi.mock('@/components/Header', () => ({
  Header: ({ title, actions }: { title: string; actions?: ReactNode }) => (
    <header>
      <h1>{title}</h1>
      {actions}
    </header>
  ),
}));

vi.mock('@/components/LoadingState', () => ({
  LoadingSkeleton: () => <div>Loading</div>,
  LoadingState: () => <div>Loading</div>,
}));

vi.mock('@/components/checkins/CheckInForm', () => ({
  CheckInForm: () => <div>Check-in form</div>,
}));

vi.mock('@/components/checkins/CheckInHistory', () => ({
  CheckInHistory: () => <div>Check-in history</div>,
}));

vi.mock('@/components/checkins/Day7ReviewCard', () => ({
  Day7ReviewCard: () => <div>Day 7 review</div>,
}));

vi.mock('@/components/checkins/TrendChart', () => ({
  TrendChart: () => <div>Trend chart</div>,
}));

vi.mock('@/lib/api', () => ({
  apiClient: {
    getCheckIns: vi.fn(),
    getProfileGoals: vi.fn(),
    getCompounds: vi.fn(),
    checkOverlap: vi.fn(),
    createCheckIn: vi.fn(),
  },
}));

import { apiClient } from '@/lib/api';

describe('/checkins empty state (B5)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    useProfileMock.mockReturnValue({
      currentProfileId: 'p-1',
      profiles: [
        { id: 'p-1', displayName: 'Riley Vega', weight: 80 },
      ],
    });
    vi.mocked(apiClient.getCheckIns).mockResolvedValue([]);
    vi.mocked(apiClient.getProfileGoals).mockResolvedValue([]);
    vi.mocked(apiClient.getCompounds).mockResolvedValue([]);
    vi.mocked(apiClient.checkOverlap).mockResolvedValue([]);
  });

  it('uses the active profile name in the empty-state title', async () => {
    render(<CheckInsPage />);

    await waitFor(() => {
      expect(screen.getByText('No check-ins logged for Riley Vega yet')).toBeInTheDocument();
    });
    expect(screen.getByText('Record the first to start a baseline.')).toBeInTheDocument();
  });

  it('falls back to "this profile" when no profile is found in the list', async () => {
    useProfileMock.mockReturnValue({
      currentProfileId: 'missing',
      profiles: [],
    });

    render(<CheckInsPage />);

    await waitFor(() => {
      expect(screen.queryByText(/Your protocol has no observations yet/)).not.toBeInTheDocument();
    });
    expect(
      screen.queryByText('Record the first check-in so runs have reality to compare against.')
    ).not.toBeInTheDocument();
  });
});
