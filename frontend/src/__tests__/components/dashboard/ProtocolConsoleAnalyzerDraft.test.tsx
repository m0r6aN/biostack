import { ProtocolConsole } from '@/components/dashboard/ProtocolConsole';
import ProfilesPage from '@/app/profiles/page';
import { ANALYZER_PROTOCOL_DRAFT_KEY, readAnalyzerProtocolDraft, saveAnalyzerProtocolDraft } from '@/lib/analyzerStorage';
import { apiClient } from '@/lib/api';
import type { CreateProfileRequest, PersonProfile } from '@/lib/types';
import { act, fireEvent, render, screen, waitFor } from '@testing-library/react';
import type { ReactNode } from 'react';
import { afterEach, beforeEach, describe, expect, it, vi, type MockInstance } from 'vitest';

const pushMock = vi.fn();
vi.mock('next/navigation', () => ({
  useRouter: () => ({ push: pushMock }),
}));

const profileState: { currentProfileId: string | null; profiles: PersonProfile[] } = { currentProfileId: null, profiles: [] };
const setProfiles = vi.fn();
const setCurrentProfileId = vi.fn();
vi.mock('@/lib/context', () => ({
  useProfile: () => ({
    currentProfileId: profileState.currentProfileId,
    profiles: profileState.profiles,
    setProfiles,
    setCurrentProfileId,
  }),
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
vi.mock('@/components/ProfileSwitcher', () => ({ ProfileSwitcher: () => <div>Profile switcher</div> }));
vi.mock('@/components/LoadingState', () => ({ LoadingSkeleton: () => <div>Loading console</div> }));
vi.mock('@/components/ErrorState', () => ({ ErrorState: ({ message }: { message: string }) => <div>{message}</div> }));
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

function analyzerEvents(): string[] {
  const names: string[] = [];
  window.addEventListener('biostack:analyzer_event', (event) => {
    names.push((event as CustomEvent<{ eventName: string }>).detail.eventName);
  });
  return names;
}

describe('ProtocolConsole — analyzer draft continuation after sign-in', () => {
  let consoleError: MockInstance;

  beforeEach(() => {
    vi.clearAllMocks();
    window.localStorage.clear();
    window.history.replaceState({}, '', '/protocol-console');
    profileState.currentProfileId = null;
    profileState.profiles = [];
    vi.mocked(apiClient.getProfiles).mockResolvedValue([]);
    vi.mocked(apiClient.getCompounds).mockResolvedValue([]);
    vi.mocked(apiClient.getCheckIns).mockResolvedValue([]);
    vi.mocked(apiClient.getTimeline).mockResolvedValue([]);
    vi.mocked(apiClient.getProfileGoals).mockResolvedValue([]);
    vi.mocked(apiClient.getCurrentStackIntelligence).mockResolvedValue(null as never);
    vi.mocked(apiClient.getProtocolConsole).mockResolvedValue(null as never);
    vi.mocked(apiClient.checkOverlap).mockResolvedValue([]);
    vi.mocked(apiClient.createCompound).mockResolvedValue({} as never);
    vi.mocked(apiClient.createProfile).mockResolvedValue(profile);
    consoleError = vi.spyOn(console, 'error').mockImplementation(() => {});
  });

  afterEach(() => {
    consoleError.mockRestore();
  });

  describe('first profile (the sign-in promise previously dead-ended here)', () => {
    it('shows the recovered draft with a continuation into profile setup instead of a bare empty state', async () => {
      seedDraft();
      const events = analyzerEvents();

      render(<ProtocolConsole />);

      expect(await screen.findByText('Your analyzed protocol is waiting.')).toBeInTheDocument();
      expect(screen.getByTestId('analyzer-draft-original')).toHaveTextContent('BPC-157');
      expect(screen.getByTestId('analyzer-draft-alternative')).toHaveTextContent('TB-500');
      expect(screen.queryByRole('checkbox')).not.toBeInTheDocument();
      expect(screen.queryByText("Let's set up your first profile")).not.toBeInTheDocument();

      fireEvent.click(screen.getByRole('button', { name: 'Continue Profile Setup' }));
      expect(pushMock).toHaveBeenCalledWith('/profiles?bootstrap=analyzer');
      expect(apiClient.createCompound).not.toHaveBeenCalled();
      expect(events).toContain('analyzer_draft_recovered');
    });

    it('keeps the original first-profile empty state when there is no draft', async () => {
      render(<ProtocolConsole />);

      expect(await screen.findByText("Let's set up your first profile")).toBeInTheDocument();
      fireEvent.click(screen.getByRole('button', { name: 'Create profile' }));
      expect(pushMock).toHaveBeenCalledWith('/profiles');
    });

    it('asks the user to choose a profile when profiles exist but none is selected', async () => {
      seedDraft();
      profileState.profiles = [profile];

      render(<ProtocolConsole />);

      expect(await screen.findByText('Your analyzed protocol is waiting.')).toBeInTheDocument();
      expect(screen.getByText('Profile switcher')).toBeInTheDocument();
      expect(screen.getByText(/Choose a profile above/)).toBeInTheDocument();
      expect(screen.queryByRole('button', { name: 'Continue Profile Setup' })).not.toBeInTheDocument();
      expect(apiClient.createCompound).not.toHaveBeenCalled();
    });

    it('continues a partial first-profile import on its created target without duplicate profiles or compounds', async () => {
      seedDraft();
      window.history.replaceState({}, '', '/profiles?bootstrap=analyzer');
      vi.mocked(apiClient.createCompound).mockResolvedValueOnce({} as never).mockRejectedValueOnce(new Error('partial'));
      const setup = render(<ProfilesPage />);
      fireEvent.click(await screen.findByRole('button', { name: 'Save profile' }));
      expect(await screen.findByRole('alert')).toHaveTextContent('Profile Clint was created');
      expect(screen.queryByRole('button', { name: 'Save profile' })).not.toBeInTheDocument();
      const continuation = screen.getByRole('link', { name: 'Continue setup on Clint' });
      expect(continuation).toHaveAttribute('href', '/protocol-console');
      fireEvent.click(continuation);
      expect(setCurrentProfileId).toHaveBeenLastCalledWith('profile-1');
      setup.unmount();

      // Follow the actual continuation route with the server's successful first addition.
      profileState.currentProfileId = 'profile-1';
      profileState.profiles = [profile];
      vi.mocked(apiClient.getProfiles).mockResolvedValue([profile]);
      vi.mocked(apiClient.getCompounds).mockResolvedValue([{ name: 'BPC-157', personId: 'profile-1', status: 'Active' }] as never);
      render(<ProtocolConsole />);
      fireEvent.click(await screen.findByRole('button', { name: 'Add 1 compound to Clint' }));
      await waitFor(() => expect(readAnalyzerProtocolDraft()?.importStatus.status).toBe('imported'));
      expect(apiClient.createProfile).toHaveBeenCalledTimes(1);
      expect(vi.mocked(apiClient.createCompound).mock.calls.map(([id, item]) => [id, item.name])).toEqual([
        ['profile-1', 'BPC-157'], ['profile-1', 'NAD+'], ['profile-1', 'NAD+'],
      ]);
    });
  });

  describe('existing profile', () => {
    beforeEach(() => {
      profileState.currentProfileId = 'profile-1';
      profileState.profiles = [profile];
      vi.mocked(apiClient.getProfiles).mockResolvedValue([profile]);
    });

    it('offers the user-entered compounds for explicit confirmation and never adds anything on its own', async () => {
      seedDraft();
      vi.mocked(apiClient.getCompounds).mockResolvedValue([
        { id: 'c1', personId: 'profile-1', name: 'nad+', category: 'Unknown', startDate: 's', endDate: null, status: 'Active', notes: '', sourceType: 'Manual' },
      ]);

      render(<ProtocolConsole />);

      const confirm = await screen.findByRole('button', { name: 'Add 1 compound to Clint' });
      expect(screen.getByTestId('analyzer-draft-original')).toHaveTextContent('NAD+');
      expect(screen.getByTestId('analyzer-draft-original')).toHaveTextContent('already on this profile');
      expect(apiClient.createCompound).not.toHaveBeenCalled();
      expect(readAnalyzerProtocolDraft()?.importStatus.status).toBe('pending');

      fireEvent.click(confirm);

      await waitFor(() => expect(readAnalyzerProtocolDraft()?.importStatus.status).toBe('imported'));
      expect(apiClient.createCompound).toHaveBeenCalledTimes(1);
      const [profileId, compound] = vi.mocked(apiClient.createCompound).mock.calls[0];
      expect(profileId).toBe('profile-1');
      expect(compound).toMatchObject({ name: 'BPC-157', status: 'Active', source: 'Protocol Analyzer', goal: 'Recovery' });
      expect(compound.notes).toContain('as entered: 500 mcg · daily · 4 weeks');
      expect(readAnalyzerProtocolDraft()?.importStatus.importedProfileIds).toEqual(['profile-1']);
      expect(await screen.findByRole('status')).toHaveTextContent('Added 1 compound from your analysis to Clint');
      expect(screen.queryByText('Your analyzed protocol is waiting.')).not.toBeInTheDocument();
      expect(apiClient.getCompounds).toHaveBeenCalledTimes(2);
    });

    it('keeps the draft pending and reports the failure when adding a compound fails', async () => {
      seedDraft();
      vi.mocked(apiClient.createCompound).mockResolvedValueOnce({} as never).mockRejectedValueOnce(new Error('boom'));

      render(<ProtocolConsole />);
      fireEvent.click(await screen.findByRole('button', { name: 'Add 2 compounds to Clint' }));

      expect(await screen.findByRole('alert')).toHaveTextContent('Some compounds could not be added');
      expect(readAnalyzerProtocolDraft()?.importStatus.status).toBe('pending');
      expect(screen.getByText('Your analyzed protocol is waiting.')).toBeInTheDocument();
      expect(apiClient.createCompound).toHaveBeenCalledTimes(2);
    });

    it('lets the user defer without losing the draft', async () => {
      seedDraft();
      const events = analyzerEvents();

      render(<ProtocolConsole />);
      fireEvent.click(await screen.findByRole('button', { name: 'Not now' }));

      expect(screen.queryByText('Your analyzed protocol is waiting.')).not.toBeInTheDocument();
      expect(readAnalyzerProtocolDraft()?.importStatus.status).toBe('pending');
      expect(apiClient.createCompound).not.toHaveBeenCalled();
      expect(events).toContain('analyzer_draft_dismissed');
    });

    it('does not re-offer a draft already imported into a profile', async () => {
      seedDraft();
      const stored = JSON.parse(window.localStorage.getItem(ANALYZER_PROTOCOL_DRAFT_KEY)!);
      window.localStorage.setItem(
        ANALYZER_PROTOCOL_DRAFT_KEY,
        JSON.stringify({ ...stored, importStatus: { status: 'imported', importedAt: 'x', importedProfileIds: ['profile-0'] } })
      );

      render(<ProtocolConsole />);

      expect(await screen.findByText('Active Compounds')).toBeInTheDocument();
      expect(screen.queryByText('Your analyzed protocol is waiting.')).not.toBeInTheDocument();
    });

    it('tolerates corrupted device storage', async () => {
      window.localStorage.setItem(ANALYZER_PROTOCOL_DRAFT_KEY, '{"schemaVersion":1,"protocol":[{"compoundName":"BPC-157","dose":"500"}]}');

      render(<ProtocolConsole />);

      expect(await screen.findByText('Active Compounds')).toBeInTheDocument();
      expect(screen.queryByText('Your analyzed protocol is waiting.')).not.toBeInTheDocument();
      expect(apiClient.createCompound).not.toHaveBeenCalled();
    });

    it('waits for the selected profile compound list even when profile discovery finishes first', async () => {
      seedDraft();
      let finish!: () => void;
      vi.mocked(apiClient.getCompounds).mockImplementationOnce(() => new Promise((resolve) => {
        finish = () => resolve([{ name: 'BPC-157' }, { name: 'NAD+' }] as never);
      }));
      render(<ProtocolConsole />);
      await waitFor(() => expect(setProfiles).toHaveBeenCalledWith([profile]));
      expect(screen.queryByRole('button', { name: /^Add \d/ })).not.toBeInTheDocument();
      expect(apiClient.createCompound).not.toHaveBeenCalled();
      await act(async () => { finish(); });
      expect(await screen.findByText(/Everything you entered is already on Clint/)).toBeInTheDocument();
    });

    it('does not offer an import using the previous profile list while a new profile loads', async () => {
      seedDraft();
      const secondProfile = { ...profile, id: 'profile-2', displayName: 'Second' };
      profileState.profiles = [profile, secondProfile];
      let finish!: () => void;
      vi.mocked(apiClient.getCompounds).mockResolvedValueOnce([]).mockImplementationOnce(() => new Promise((resolve) => {
        finish = () => resolve([{ name: 'NAD+' }] as never);
      }));
      const view = render(<ProtocolConsole />);
      expect(await screen.findByRole('button', { name: 'Add 2 compounds to Clint' })).toBeInTheDocument();
      profileState.currentProfileId = 'profile-2';
      view.rerender(<ProtocolConsole />);
      expect(screen.queryByRole('button', { name: /^Add \d/ })).not.toBeInTheDocument();
      await act(async () => { finish(); });
      fireEvent.click(await screen.findByRole('button', { name: 'Add 1 compound to Second' }));
      await waitFor(() => expect(readAnalyzerProtocolDraft()?.importStatus.status).toBe('imported'));
      expect(apiClient.createCompound).toHaveBeenCalledTimes(1);
      expect(apiClient.createCompound).toHaveBeenCalledWith('profile-2', expect.objectContaining({ name: 'BPC-157' }));
    });

    it('ignores an old profile response that arrives after switching profiles', async () => {
      seedDraft();
      const secondProfile = { ...profile, id: 'profile-2', displayName: 'Second' };
      profileState.profiles = [profile, secondProfile];
      let finishOld!: () => void;
      vi.mocked(apiClient.getCompounds).mockImplementationOnce(() => new Promise((resolve) => {
        finishOld = () => resolve([]);
      })).mockResolvedValue([{ name: 'NAD+' }] as never);
      const view = render(<ProtocolConsole />);
      await waitFor(() => expect(apiClient.getCompounds).toHaveBeenCalledWith('profile-1'));
      profileState.currentProfileId = 'profile-2';
      view.rerender(<ProtocolConsole />);
      expect(await screen.findByRole('button', { name: 'Add 1 compound to Second' })).toBeInTheDocument();
      await act(async () => { finishOld(); });
      expect(screen.getByRole('button', { name: 'Add 1 compound to Second' })).toBeInTheDocument();
      expect(screen.queryByRole('button', { name: 'Add 2 compounds to Second' })).not.toBeInTheDocument();
    });

    it('keeps a replacement draft visible when an older import completes', async () => {
      seedDraft();
      let finish!: () => void;
      vi.mocked(apiClient.createCompound).mockImplementationOnce(() => new Promise((resolve) => { finish = () => resolve({} as never); }));
      render(<ProtocolConsole />);
      fireEvent.click(await screen.findByRole('button', { name: 'Add 2 compounds to Clint' }));
      let replacement!: ReturnType<typeof seedDraft>;
      await act(async () => {
        replacement = saveAnalyzerProtocolDraft({ sourceAnalysisId: 'analysis-new', goal: 'New entries', protocol: [{ ...original[0], compoundName: 'New entry' }], optimizedProtocol: [] });
        finish();
      });
      expect(await screen.findByRole('button', { name: 'Add 1 compound to Clint' })).toBeInTheDocument();
      expect(screen.getByTestId('analyzer-draft-original')).toHaveTextContent('New entry');
      expect(readAnalyzerProtocolDraft()).toEqual(replacement);
      expect(vi.mocked(apiClient.createCompound).mock.calls.map(([, item]) => item.name)).toEqual(['BPC-157', 'NAD+']);
    });

    it('shows a newly saved draft after the prior draft was deferred', async () => {
      seedDraft();
      render(<ProtocolConsole />);
      fireEvent.click(await screen.findByRole('button', { name: 'Not now' }));
      expect(screen.queryByText('Your analyzed protocol is waiting.')).not.toBeInTheDocument();
      act(() => { seedDraft(); });
      expect(await screen.findByText('Your analyzed protocol is waiting.')).toBeInTheDocument();
    });
  });
});
