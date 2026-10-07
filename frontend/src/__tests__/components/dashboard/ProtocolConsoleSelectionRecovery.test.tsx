import { ProtocolConsole } from '@/components/dashboard/ProtocolConsole';
import { ProfileProvider, useProfile } from '@/lib/context';
import { readAnalyzerProtocolDraft, saveAnalyzerProtocolDraft } from '@/lib/analyzerStorage';
import { apiClient } from '@/lib/api';
import type { CreateProfileRequest, PersonProfile } from '@/lib/types';
import { act, fireEvent, render, screen, waitFor } from '@testing-library/react';
import type { ReactNode } from 'react';
import { afterEach, beforeEach, describe, expect, it, vi, type MockInstance } from 'vitest';

const pushMock = vi.fn();
vi.mock('next/navigation', () => ({
  useRouter: () => ({ push: pushMock }),
}));

vi.mock('@/lib/api', () => ({
  ApiError: class ApiError extends Error {
    upgradeRequired?: boolean;
  },
  apiClient: {
    getProfiles: vi.fn(),
    getCompounds: vi.fn(),
    getCheckIns: vi.fn(),
    getTimeline: vi.fn(),
    getProfileGoals: vi.fn(),
    getCurrentStackIntelligence: vi.fn(),
    getProtocolConsole: vi.fn(),
    checkOverlap: vi.fn(),
    createCompound: vi.fn(),
    createProfile: vi.fn(),
    setProfileGoals: vi.fn(),
  },
}));

vi.mock('@/lib/flags', () => ({ isEnabled: () => false }));
vi.mock('@/lib/settings', () => ({ useSettings: () => ({ settings: { weightUnit: 'metric' } }) }));
vi.mock('@/components/profiles/ProfileForm', () => ({
  ProfileForm: ({ onSubmit }: { onSubmit: (data: CreateProfileRequest) => Promise<void> }) => (
    <button onClick={() => void onSubmit({ displayName: 'Clint', sex: 'Male', weight: 80, notes: '' })}>Save profile</button>
  ),
}));

vi.mock('@/components/Header', () => ({
  Header: ({ title, actions }: { title: string; actions?: ReactNode }) => (
    <header>
      <h1>{title}</h1>
      {actions}
    </header>
  ),
}));

vi.mock('@/components/LoadingState', () => ({ LoadingSkeleton: () => <div>Loading console</div> }));
vi.mock('@/components/dashboard/ActiveCompoundsCard', () => ({ ActiveCompoundsCard: () => null }));
vi.mock('@/components/dashboard/ActiveGoalsCard', () => ({ ActiveGoalsCard: () => null }));
vi.mock('@/components/dashboard/CohesionTimelinePanel', () => ({ CohesionTimelinePanel: () => null }));
vi.mock('@/components/dashboard/DriftRegimePanel', () => ({ DriftRegimePanel: () => null }));
vi.mock('@/components/dashboard/LatestCheckInCard', () => ({ LatestCheckInCard: () => null }));
vi.mock('@/components/dashboard/ObservationSignalsPanel', () => ({ ObservationSignalsPanel: () => null }));
vi.mock('@/components/dashboard/OverlapFlagsBanner', () => ({ OverlapFlagsBanner: () => null }));
vi.mock('@/components/dashboard/PatternMemoryPanel', () => ({ PatternMemoryPanel: () => null }));
vi.mock('@/components/dashboard/ProtocolConsoleOverview', () => ({ ProtocolConsoleOverview: () => null }));
vi.mock('@/components/dashboard/SequenceExpectationPanel', () => ({ SequenceExpectationPanel: () => null }));
vi.mock('@/components/dashboard/StatCard', () => ({ StatCard: ({ title }: { title: string }) => <div>{title}</div> }));
vi.mock('@/components/dashboard/TimelineSnapshot', () => ({ TimelineSnapshot: () => null }));
vi.mock('@/components/mission/NextObservationCard', () => ({ NextObservationCard: () => null }));
vi.mock('@/components/mission/ObservationDebtInbox', () => ({ ObservationDebtInbox: () => null }));
vi.mock('@/components/mission/OperatingStateHero', () => ({ OperatingStateHero: () => null }));
vi.mock('@/components/mission/ProtocolWeather', () => ({ ProtocolWeather: () => null }));
vi.mock('@/components/mission/StackClarityMeter', () => ({ StackClarityMeter: () => null }));
vi.mock('@/components/mission/StackGraphMini', () => ({ StackGraphMini: () => null }));

const profile: PersonProfile = {
  id: 'profile-1',
  displayName: 'Clint',
  sex: 'Male',
  weight: 80,
  notes: '',
  createdAtUtc: '2026-09-01T00:00:00.000Z',
  updatedAtUtc: '2026-09-01T00:00:00.000Z',
};

const original = [
  { compoundName: 'BPC-157', dose: 500, unit: 'mcg', frequency: 'daily', duration: '4 weeks' },
  { compoundName: 'NAD+', dose: 100, unit: 'mg', frequency: 'daily', duration: '' },
];
const alternative = [{ compoundName: 'TB-500', dose: 2000, unit: 'mcg', frequency: 'twice-weekly', duration: '' }];

function seedDraft() {
  return saveAnalyzerProtocolDraft({ sourceAnalysisId: 'analysis-001', goal: 'Recovery', protocol: original, optimizedProtocol: alternative });
}

function SelectionProbe() {
  const { currentProfileId, setCurrentProfileId } = useProfile();
  return <>
    <output data-testid="selection">{currentProfileId ?? 'none'}</output>
    <button onClick={() => setCurrentProfileId(profile.id)}>Choose valid profile</button>
  </>;
}

function renderConsole() {
  return render(<ProfileProvider><SelectionProbe /><ProtocolConsole /></ProfileProvider>);
}

describe('ProtocolConsole - profile discovery and selection recovery', () => {
  let consoleError: MockInstance;

  beforeEach(() => {
    vi.resetAllMocks();
    localStorage.clear();
    localStorage.setItem('currentProfileId', 'stale-profile');
    vi.mocked(apiClient.getProfiles).mockResolvedValue([profile]);
    vi.mocked(apiClient.getCompounds).mockImplementation(async (id) => {
      if (id === 'stale-profile') throw new Error('404 profile not found');
      return [];
    });
    vi.mocked(apiClient.getCheckIns).mockResolvedValue([]);
    vi.mocked(apiClient.getTimeline).mockResolvedValue([]);
    vi.mocked(apiClient.getProfileGoals).mockResolvedValue([]);
    vi.mocked(apiClient.getCurrentStackIntelligence).mockResolvedValue(null as never);
    vi.mocked(apiClient.getProtocolConsole).mockResolvedValue(null as never);
    vi.mocked(apiClient.checkOverlap).mockResolvedValue([]);
    vi.mocked(apiClient.createCompound).mockResolvedValue({} as never);
    consoleError = vi.spyOn(console, 'error').mockImplementation(() => {});
  });

  afterEach(() => consoleError.mockRestore());

  it.each([false, true])('waits for discovery before offering setup without a selection (pending draft: %s)', async (withDraft) => {
    localStorage.removeItem('currentProfileId');
    const draft = withDraft ? seedDraft() : null;
    let finish!: () => void;
    vi.mocked(apiClient.getProfiles).mockImplementation(() => new Promise(resolve => { finish = () => resolve([profile]); }));
    renderConsole();

    expect(screen.getByText('Loading console')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Create profile' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Continue Profile Setup' })).not.toBeInTheDocument();
    await act(async () => { finish(); });

    expect(screen.getByRole('button', { name: /Select Profile/ })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Create profile' })).not.toBeInTheDocument();
    expect(readAnalyzerProtocolDraft()).toEqual(draft);
    expect(apiClient.createCompound).not.toHaveBeenCalled();
  });

  it.each([false, true])('retries failed discovery before deciding whether setup is needed (pending draft: %s)', async (withDraft) => {
    localStorage.removeItem('currentProfileId');
    const draft = withDraft ? seedDraft() : null;
    let finishRetry!: () => void;
    vi.mocked(apiClient.getProfiles).mockRejectedValueOnce(new Error('network unavailable'))
      .mockImplementationOnce(() => new Promise(resolve => { finishRetry = () => resolve([]); }));
    renderConsole();

    expect(await screen.findByText('Failed to load profiles')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Create profile' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Continue Profile Setup' })).not.toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Try Again' }));
    expect(screen.getByText('Loading console')).toBeInTheDocument();
    expect(apiClient.getProfiles).toHaveBeenCalledTimes(2);
    await act(async () => { finishRetry(); });

    expect(screen.getByRole('button', { name: withDraft ? 'Continue Profile Setup' : 'Create profile' })).toBeInTheDocument();
    expect(readAnalyzerProtocolDraft()).toEqual(draft);
    expect(apiClient.createCompound).not.toHaveBeenCalled();
  });

  it('keeps a discovery failure visible even when the selected profile data succeeds, and retries the failed request', async () => {
    localStorage.setItem('currentProfileId', profile.id);
    vi.mocked(apiClient.getProfiles).mockRejectedValueOnce(new Error('network unavailable')).mockResolvedValueOnce([profile]);
    renderConsole();

    expect(await screen.findByText('Failed to load profiles')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Try Again' }));

    await waitFor(() => expect(screen.getByTestId('selection')).toHaveTextContent(profile.id));
    await waitFor(() => expect(screen.queryByText('Failed to load profiles')).not.toBeInTheDocument());
    expect(apiClient.getProfiles).toHaveBeenCalledTimes(2);
    expect(localStorage.getItem('currentProfileId')).toBe(profile.id);
  });

  it('recovers a draft after another account was selected, then imports only into the explicitly chosen valid profile', async () => {
    const draft = seedDraft();
    renderConsole();

    expect(await screen.findByText(/Choose a profile above/)).toBeInTheDocument();
    expect(screen.queryByText('Failed to load protocol console data')).not.toBeInTheDocument();
    await waitFor(() => expect(localStorage.getItem('currentProfileId')).toBeNull());
    expect(readAnalyzerProtocolDraft()).toEqual(draft);
    expect(apiClient.createCompound).not.toHaveBeenCalled();

    fireEvent.click(screen.getByRole('button', { name: /Select Profile/ }));
    fireEvent.click(screen.getByRole('button', { name: /Clint/ }));
    fireEvent.click(await screen.findByRole('button', { name: 'Add 2 compounds to Clint' }));

    await waitFor(() => expect(readAnalyzerProtocolDraft()?.importStatus.status).toBe('imported'));
    expect(apiClient.createCompound).toHaveBeenCalledTimes(2);
    expect(vi.mocked(apiClient.createCompound).mock.calls.every(([id]) => id === profile.id)).toBe(true);
    expect(localStorage.getItem('currentProfileId')).toBe(profile.id);
  });

  it('offers first-profile setup after the remembered profile was deleted and stays recovered on remount', async () => {
    const draft = seedDraft();
    vi.mocked(apiClient.getProfiles).mockResolvedValue([]);
    const view = renderConsole();

    expect(await screen.findByRole('button', { name: 'Continue Profile Setup' })).toBeInTheDocument();
    await waitFor(() => expect(localStorage.getItem('currentProfileId')).toBeNull());
    expect(readAnalyzerProtocolDraft()).toEqual(draft);
    view.unmount();
    vi.mocked(apiClient.getCompounds).mockClear();
    renderConsole();

    expect(await screen.findByRole('button', { name: 'Continue Profile Setup' })).toBeInTheDocument();
    expect(apiClient.getCompounds).not.toHaveBeenCalled();
    expect(apiClient.createCompound).not.toHaveBeenCalled();
  });

  it('offers profile selection without an analyzer draft instead of asking an existing user to create a first profile', async () => {
    renderConsole();

    expect(await screen.findByText('Choose a profile')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Select Profile/ })).toBeInTheDocument();
    expect(screen.getByTestId('selection')).toHaveTextContent('none');
    expect(screen.queryByText("Let's set up your first profile")).not.toBeInTheDocument();
    await waitFor(() => expect(localStorage.getItem('currentProfileId')).toBeNull());
  });

  it('does not discard a remembered selection when profile discovery fails', async () => {
    vi.mocked(apiClient.getProfiles).mockRejectedValue(new Error('network unavailable'));
    renderConsole();

    await waitFor(() => expect(consoleError).toHaveBeenCalled());
    expect(screen.getByTestId('selection')).toHaveTextContent('stale-profile');
    expect(localStorage.getItem('currentProfileId')).toBe('stale-profile');
  });

  it('keeps a valid selection made while profile discovery is pending', async () => {
    let finish!: () => void;
    vi.mocked(apiClient.getProfiles).mockImplementation(() => new Promise(resolve => { finish = () => resolve([profile]); }));
    renderConsole();
    fireEvent.click(screen.getByRole('button', { name: 'Choose valid profile' }));
    await act(async () => { finish(); });

    await waitFor(() => expect(screen.getByTestId('selection')).toHaveTextContent(profile.id));
    expect(localStorage.getItem('currentProfileId')).toBe(profile.id);
    expect(screen.queryByText('Failed to load protocol console data')).not.toBeInTheDocument();
  });

  it('ignores a stale-profile failure that arrives after recovery and selection', async () => {
    seedDraft();
    let failOld!: () => void;
    vi.mocked(apiClient.getCompounds).mockImplementation(id => id === 'stale-profile'
      ? new Promise((_, reject) => { failOld = () => reject(new Error('late 404')); })
      : Promise.resolve([]));
    renderConsole();
    await waitFor(() => expect(screen.getByTestId('selection')).toHaveTextContent('none'));
    fireEvent.click(screen.getByRole('button', { name: 'Choose valid profile' }));
    expect(await screen.findByRole('button', { name: 'Add 2 compounds to Clint' })).toBeInTheDocument();
    await act(async () => { failOld(); });

    expect(screen.getByRole('button', { name: 'Add 2 compounds to Clint' })).toBeInTheDocument();
    expect(screen.queryByText('Failed to load protocol console data')).not.toBeInTheDocument();
  });
});
