import ProfilesPage from '@/app/profiles/page';
import { ANALYZER_PROTOCOL_DRAFT_KEY, readAnalyzerProtocolDraft, saveAnalyzerProtocolDraft } from '@/lib/analyzerStorage';
import { ANONYMOUS_TOOL_PAYLOAD_KEY } from '@/lib/anonymousTools';
import { apiClient } from '@/lib/api';
import type { CreateProfileRequest } from '@/lib/types';
import { act, fireEvent, render, screen, waitFor } from '@testing-library/react';
import type { ReactNode } from 'react';
import { afterEach, beforeEach, describe, expect, it, vi, type MockInstance } from 'vitest';

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
  ProfileForm: ({ onSubmit }: { onSubmit: (data: CreateProfileRequest) => Promise<void> }) => (
    <div>
      <p>Profile form</p>
      <button type="button" onClick={() => void onSubmit({ displayName: 'Clint', sex: 'Male', weight: 80, notes: '' })}>
        Save profile
      </button>
    </div>
  ),
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

const original = [
  { compoundName: 'BPC-157', dose: 500, unit: 'mcg', frequency: 'daily', duration: '4 weeks' },
  { compoundName: 'NAD+', dose: 100, unit: 'mg', frequency: 'daily', duration: '' },
];
const alternative = [{ compoundName: 'TB-500', dose: 2000, unit: 'mcg', frequency: 'twice-weekly', duration: '' }];

function seedDraft() {
  return saveAnalyzerProtocolDraft({ sourceAnalysisId: 'analysis-001', goal: 'Recovery', protocol: original, optimizedProtocol: alternative });
}

function seedToolsPayload() {
  window.localStorage.setItem(
    ANONYMOUS_TOOL_PAYLOAD_KEY,
    JSON.stringify({
      schemaVersion: 1,
      savedCalculations: [{ id: 'local-tool-1', contentHash: 'h', calculatorType: 'dose', substances: ['NAD+'], inputs: {}, outputs: {}, reconstitutionInstructions: [], storageInstructions: [], compatibilityFindings: [], source: 'local_device_bootstrap', createdAt: 'c', updatedAt: 'u' }],
      savedSetups: [],
      savedCompatibilityChecks: [],
      draftStackItems: [{ id: 'local-draft-1', name: 'NAD+', sourceArtifactId: 'local-tool-1', source: 'local_device_bootstrap', createdAt: 'c' }],
      importStatus: { status: 'pending', importedProfileIds: [] },
      createdAt: 'c',
      updatedAt: 'u',
    })
  );
}

describe('ProfilesPage — analyzer draft continuation (first profile)', () => {
  let consoleError: MockInstance;

  beforeEach(() => {
    vi.clearAllMocks();
    window.localStorage.clear();
    window.history.replaceState({}, '', '/profiles');
    vi.mocked(apiClient.getProfiles).mockResolvedValue([]);
    vi.mocked(apiClient.createProfile).mockResolvedValue({ id: 'profile-1', displayName: 'Clint' } as never);
    vi.mocked(apiClient.createCompound).mockResolvedValue({} as never);
    // jsdom cannot navigate; the post-create redirect logs a "not implemented" error we do not care about here.
    consoleError = vi.spyOn(console, 'error').mockImplementation(() => {});
  });

  afterEach(() => {
    consoleError.mockRestore();
  });

  it('surfaces the saved draft as reviewable work instead of a blank first-profile state (previously write-only)', async () => {
    seedDraft();

    render(<ProfilesPage />);

    expect(await screen.findByText('Your analyzed protocol is waiting.')).toBeInTheDocument();
    const originalList = screen.getByTestId('analyzer-draft-original');
    expect(originalList).toHaveTextContent('BPC-157');
    expect(originalList).toHaveTextContent('500 mcg · daily · 4 weeks');
    expect(originalList).not.toHaveTextContent('TB-500');
    expect(screen.getByText('Analyzer alternative — comparison only, not applied')).toBeInTheDocument();
    expect(screen.getByTestId('analyzer-draft-alternative')).toHaveTextContent('TB-500');
    expect(screen.queryByText('Profile form')).not.toBeInTheDocument();
    expect(screen.queryByText('Create your first profile to get started')).not.toBeInTheDocument();
    expect(apiClient.createCompound).not.toHaveBeenCalled();
  });

  it('opens profile setup automatically for the analyzer bootstrap and keeps the review visible', async () => {
    seedDraft();
    window.history.replaceState({}, '', '/profiles?bootstrap=analyzer');

    render(<ProfilesPage />);

    expect(await screen.findByText('Profile form')).toBeInTheDocument();
    expect(screen.getByText('Your analyzed protocol is waiting.')).toBeInTheDocument();
    expect(screen.getByRole('checkbox')).toBeChecked();
    expect(apiClient.createCompound).not.toHaveBeenCalled();
  });

  it('adds only the user-entered compounds on profile creation and marks the draft imported', async () => {
    seedDraft();
    window.history.replaceState({}, '', '/profiles?bootstrap=analyzer');

    render(<ProfilesPage />);
    fireEvent.click(await screen.findByRole('button', { name: 'Save profile' }));

    await waitFor(() => expect(readAnalyzerProtocolDraft()?.importStatus.status).toBe('imported'));
    expect(apiClient.createProfile).toHaveBeenCalledTimes(1);
    const created = vi.mocked(apiClient.createCompound).mock.calls.map(([profileId, compound]) => ({ profileId, ...compound }));
    expect(created.map((item) => item.name)).toEqual(['BPC-157', 'NAD+']);
    expect(created.every((item) => item.profileId === 'profile-1' && item.source === 'Protocol Analyzer' && item.status === 'Active')).toBe(true);
    expect(created[0].notes).toContain('as entered: 500 mcg · daily · 4 weeks');
    expect(created[0].notes).toContain('Recorded exactly as you wrote it');
    expect(readAnalyzerProtocolDraft()?.importStatus.importedProfileIds).toEqual(['profile-1']);
    expect(setCurrentProfileId).toHaveBeenCalledWith('profile-1');
  });

  it('does not add anything when the reviewer unchecks the import, and keeps the draft for later', async () => {
    seedDraft();
    window.history.replaceState({}, '', '/profiles?bootstrap=analyzer');

    render(<ProfilesPage />);
    fireEvent.click(await screen.findByRole('checkbox'));
    fireEvent.click(screen.getByRole('button', { name: 'Save profile' }));

    await waitFor(() => expect(apiClient.createProfile).toHaveBeenCalledTimes(1));
    await waitFor(() => expect(setCurrentProfileId).toHaveBeenCalledWith('profile-1'));
    expect(apiClient.createCompound).not.toHaveBeenCalled();
    expect(readAnalyzerProtocolDraft()?.importStatus.status).toBe('pending');
  });

  it('keeps the draft pending and shows the failure when a compound cannot be added', async () => {
    seedDraft();
    window.history.replaceState({}, '', '/profiles?bootstrap=analyzer');
    vi.mocked(apiClient.createCompound).mockRejectedValueOnce(new Error('boom'));

    render(<ProfilesPage />);
    fireEvent.click(await screen.findByRole('button', { name: 'Save profile' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Profile Clint was created, but setup could not finish');
    expect(readAnalyzerProtocolDraft()?.importStatus.status).toBe('pending');
    expect(setCurrentProfileId).toHaveBeenCalledWith('profile-1');
    expect(screen.queryByText('Profile form')).not.toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Continue setup on Clint' })).toHaveAttribute('href', '/protocol-console');
  });

  it('ignores corrupted device storage and falls back to the normal empty state', async () => {
    window.localStorage.setItem(ANALYZER_PROTOCOL_DRAFT_KEY, JSON.stringify({ schemaVersion: 1, id: 'x', protocol: 'not-an-array' }));
    window.history.replaceState({}, '', '/profiles?bootstrap=analyzer');

    render(<ProfilesPage />);

    expect(await screen.findByText('Create your first profile to get started')).toBeInTheDocument();
    expect(screen.queryByText('Your analyzed protocol is waiting.')).not.toBeInTheDocument();
    expect(screen.queryByText('Profile form')).not.toBeInTheDocument();
  });

  it('does not re-offer a draft that was already imported', async () => {
    seedDraft();
    const stored = JSON.parse(window.localStorage.getItem(ANALYZER_PROTOCOL_DRAFT_KEY)!);
    window.localStorage.setItem(
      ANALYZER_PROTOCOL_DRAFT_KEY,
      JSON.stringify({ ...stored, importStatus: { status: 'imported', importedAt: 'x', importedProfileIds: ['profile-0'] } })
    );

    render(<ProfilesPage />);

    expect(await screen.findByText('Create your first profile to get started')).toBeInTheDocument();
    expect(screen.queryByText('Your analyzed protocol is waiting.')).not.toBeInTheDocument();
  });

  it('preserves the existing tools bootstrap and does not duplicate names shared with the analyzer draft', async () => {
    seedToolsPayload();
    seedDraft();
    window.history.replaceState({}, '', '/profiles?bootstrap=tools');

    render(<ProfilesPage />);
    expect(await screen.findByText('Profile form')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Save profile' }));

    await waitFor(() => expect(readAnalyzerProtocolDraft()?.importStatus.status).toBe('imported'));
    const created = vi.mocked(apiClient.createCompound).mock.calls.map(([, compound]) => ({ name: compound.name, source: compound.source }));
    expect(created).toEqual([
      { name: 'NAD+', source: 'Local device bootstrap' },
      { name: 'BPC-157', source: 'Protocol Analyzer' },
    ]);
    expect(JSON.parse(window.localStorage.getItem(ANONYMOUS_TOOL_PAYLOAD_KEY)!).importStatus.status).toBe('imported');
  });

  it('still opens the tools bootstrap form when no analyzer draft exists', async () => {
    seedToolsPayload();
    window.history.replaceState({}, '', '/profiles?bootstrap=tools');

    render(<ProfilesPage />);

    expect(await screen.findByText('Profile form')).toBeInTheDocument();
    expect(screen.queryByText('Your analyzed protocol is waiting.')).not.toBeInTheDocument();
  });

  it('imports shared names when the tools payload was already imported on another profile', async () => {
    seedToolsPayload();
    const tools = JSON.parse(window.localStorage.getItem(ANONYMOUS_TOOL_PAYLOAD_KEY)!);
    tools.importStatus = { status: 'imported', importedProfileIds: ['another-profile'] };
    window.localStorage.setItem(ANONYMOUS_TOOL_PAYLOAD_KEY, JSON.stringify(tools));
    seedDraft();
    window.history.replaceState({}, '', '/profiles?bootstrap=analyzer');
    render(<ProfilesPage />);
    fireEvent.click(await screen.findByRole('button', { name: 'Save profile' }));
    await waitFor(() => expect(readAnalyzerProtocolDraft()?.importStatus.status).toBe('imported'));
    expect(vi.mocked(apiClient.createCompound).mock.calls.map(([, item]) => item.name)).toEqual(['BPC-157', 'NAD+']);
    expect(vi.mocked(apiClient.createCompound).mock.calls.every(([, item]) => item.source === 'Protocol Analyzer')).toBe(true);
  });

  it('leaves a replacement draft pending when first-profile import finishes', async () => {
    seedDraft();
    window.history.replaceState({}, '', '/profiles?bootstrap=analyzer');
    let complete!: () => void;
    vi.mocked(apiClient.createCompound).mockImplementationOnce(() => new Promise((resolve) => { complete = () => resolve({} as never); }));
    render(<ProfilesPage />);
    fireEvent.click(await screen.findByRole('button', { name: 'Save profile' }));
    await waitFor(() => expect(apiClient.createCompound).toHaveBeenCalledTimes(1));
    let replacement!: ReturnType<typeof seedDraft>;
    await act(async () => {
      replacement = seedDraft();
      complete();
    });
    await waitFor(() => expect(apiClient.createCompound).toHaveBeenCalledTimes(2));
    expect(readAnalyzerProtocolDraft()).toEqual(replacement);
    expect(readAnalyzerProtocolDraft()?.importStatus.status).toBe('pending');
  });

  it('only defers the current draft and offers a newly saved draft', async () => {
    seedDraft();
    render(<ProfilesPage />);
    fireEvent.click(await screen.findByRole('button', { name: 'Not now' }));
    expect(screen.queryByText('Your analyzed protocol is waiting.')).not.toBeInTheDocument();
    act(() => { seedDraft(); });
    expect(await screen.findByText('Your analyzed protocol is waiting.')).toBeInTheDocument();
  });
});
