import { SIDEBAR_COLLAPSED_KEY } from '@/lib/sidebarCollapse';
import CompoundsPage from '@/app/compounds/page';
import { ApiError, apiClient } from '@/lib/api';
import type { CompoundRecord, KnowledgeEntry } from '@/lib/types';
import { act, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
const profileState = vi.hoisted(() => ({ currentProfileId: 'profile-fixture' }));
vi.mock('@/lib/context', () => ({ useProfile: () => profileState }));
const { push } = vi.hoisted(() => ({ push: vi.fn() }));
const searchParamsState = vi.hoisted(() => ({ compound: null as string | null }));
vi.mock('next/navigation', () => ({
  useRouter: () => ({ push }),
  useSearchParams: () => ({ get: (key: string) => (key === 'compound' ? searchParamsState.compound : null) }),
}));
vi.mock('@/components/Header', () => ({ Header: ({ actions }: { actions?: React.ReactNode }) => <header>{actions}</header> }));
vi.mock('@/components/ActiveProfileChip', () => ({ ActiveProfileChip: () => null }));
vi.mock('@/components/knowledge/CompoundIntelligenceCard', () => ({ CompoundIntelligenceCard: ({ entry }: { entry: KnowledgeEntry }) => <div data-testid="reference-entry">{entry.canonicalName}</div> }));
vi.mock('@/components/compounds/CompoundList', () => ({
  CompoundList: ({ compounds, onSelect }: { compounds: CompoundRecord[]; onSelect?: (c: CompoundRecord) => void }) => (
    <div>{compounds.map(c => <p key={c.id} onClick={() => onSelect?.(c)}>{c.name || 'Unnamed compound'}</p>)}</div>
  ),
}));
vi.mock('@/lib/api', async importOriginal => ({
  ...await importOriginal<typeof import('@/lib/api')>(),
  apiClient: {
    getCompounds: vi.fn(),
    getAllKnowledgeCompounds: vi.fn(),
    createCompound: vi.fn(),
    getKnowledgeEntry: vi.fn(),
    updateCompound: vi.fn(),
    deleteCompound: vi.fn(),
    checkOverlap: vi.fn(),
  },
}));

const namelessCompound: CompoundRecord = {
  id: 'compound-nameless',
  personId: 'profile-fixture',
  name: '',
  category: 'Peptide',
  startDate: '2026-09-01T00:00:00Z',
  endDate: null,
  status: 'Active',
  notes: '',
  sourceType: 'Manual',
};

const namedCompound: CompoundRecord = {
  id: 'compound-named',
  personId: 'profile-fixture',
  name: 'BPC-157',
  category: 'Peptide',
  startDate: '2026-09-01T00:00:00Z',
  endDate: null,
  status: 'Active',
  notes: 'Morning dose',
  sourceType: 'Protocol Analyzer',
  goal: 'recovery',
  source: 'Protocol Analyzer',
};

beforeEach(() => {
  vi.resetAllMocks();
  profileState.currentProfileId = 'profile-fixture';
  searchParamsState.compound = null;
  window.localStorage.clear();
  vi.mocked(apiClient.checkOverlap).mockResolvedValue([]);
  vi.mocked(apiClient.getCompounds).mockResolvedValue([]);
  vi.mocked(apiClient.getAllKnowledgeCompounds).mockResolvedValue([]);
});
it('preserves entered compound after create failure and retries the same form successfully', async () => {
  vi.spyOn(console, 'error').mockImplementation(() => {});
  vi.mocked(apiClient.createCompound).mockRejectedValueOnce(new Error('fixture failure')).mockImplementationOnce(async (_, data) => ({ ...data, id: 'created-fixture' }));
  render(<CompoundsPage />);
  await screen.findByText('No Compounds Yet');
  fireEvent.click(screen.getByRole('button', { name: 'Add Compound', exact: true }));
  fireEvent.change(screen.getByLabelText('1. Select a Category'), { target: { value: 'Peptide' } });
  fireEvent.change(screen.getByLabelText('4. Optional: Manual Search/Entry'), { target: { value: 'Fixture Compound' } });
  fireEvent.change(screen.getByLabelText('Notes'), { target: { value: 'Keep this note' } });
  const submit = screen.getAllByRole('button', { name: 'Add Compound', exact: true }).at(-1)!;
  fireEvent.click(submit);
  await screen.findByText('Failed to add compound');
  expect(screen.getByLabelText('4. Optional: Manual Search/Entry')).toHaveValue('Fixture Compound');
  expect(screen.getByLabelText('Notes')).toHaveValue('Keep this note');
  fireEvent.click(screen.getAllByRole('button', { name: 'Add Compound', exact: true }).at(-1)!);
  await screen.findByText('Fixture Compound');
  expect(screen.queryByText('Failed to add compound')).not.toBeInTheDocument();
  expect(apiClient.createCompound).toHaveBeenCalledTimes(2);
  expect(apiClient.createCompound).toHaveBeenLastCalledWith('profile-fixture', expect.objectContaining({ name: 'Fixture Compound', notes: 'Keep this note' }));
  vi.restoreAllMocks();
});
it('clears a load error after the existing Try Again action succeeds', async () => {
  vi.mocked(apiClient.getCompounds).mockRejectedValueOnce(new Error('load failure')).mockResolvedValueOnce([]);
  render(<CompoundsPage />);
  await screen.findByText('Failed to load compounds');
  fireEvent.click(screen.getByRole('button', { name: 'Try Again' }));
  await waitFor(() => expect(screen.queryByText('Failed to load compounds')).not.toBeInTheDocument());
  expect(screen.getByText('No Compounds Yet')).toBeVisible();
});

it('keeps an upgrade rejection in the form and disables duplicate submission while pending', async () => {
  vi.spyOn(console, 'error').mockImplementation(() => {});
  let reject!: (error: Error) => void;
  vi.mocked(apiClient.createCompound).mockImplementationOnce(() => new Promise((_, fail) => { reject = fail; }));
  render(<CompoundsPage />);
  await screen.findByText('No Compounds Yet');
  fireEvent.click(screen.getByRole('button', { name: 'Add Compound', exact: true }));
  fireEvent.change(screen.getByLabelText('1. Select a Category'), { target: { value: 'Peptide' } });
  fireEvent.change(screen.getByLabelText('4. Optional: Manual Search/Entry'), { target: { value: 'Fixture Compound' } });
  fireEvent.click(screen.getAllByRole('button', { name: 'Add Compound', exact: true }).at(-1)!);
  expect(screen.getByRole('button', { name: 'Adding...' })).toBeDisabled();
  fireEvent.click(screen.getByRole('button', { name: 'Adding...' }));
  expect(apiClient.createCompound).toHaveBeenCalledTimes(1);
  await act(async () => reject(new ApiError(402, 'Fixture plan limit', { upgradeRequired: true })));
  expect(screen.getByRole('alert')).toHaveTextContent('Fixture plan limit');
  expect(screen.getByLabelText('4. Optional: Manual Search/Entry')).toHaveValue('Fixture Compound');
  expect(screen.getAllByRole('button', { name: 'Add Compound', exact: true }).at(-1)).toBeEnabled();
  vi.restoreAllMocks();
});

const knowledgeEntryFixture: KnowledgeEntry = {
  canonicalName: 'BPC-157',
  aliases: [],
  classification: 'Peptide',
  regulatoryStatus: 'Research',
  mechanismSummary: 'Tissue support',
  evidenceTier: 'Moderate',
  sourceReferences: [],
  notes: '',
  pathways: [],
  benefits: ['recovery'],
  pairsWellWith: [],
  avoidWith: [],
  compatibleBlends: [],
  vialCompatibility: '',
  recommendedDosage: '',
  standardDosageRange: '',
  maxReportedDose: '',
  frequency: '',
  preferredTimeOfDay: '',
  weeklyDosageSchedule: [],
  incrementalEscalationSteps: [],
  drugInteractions: [],
  optimizationProtein: '',
  optimizationCarbs: '',
  optimizationSupplements: [],
  optimizationSleep: '',
  optimizationExercise: '',
};

it('points the empty state at the library instead of leaving a blank slate', async () => {
  render(<CompoundsPage />);
  await screen.findByText('No Compounds Yet');
  expect(screen.getByText('Browse the library to see what the research says before adding anything.')).toBeVisible();
  expect(screen.getByRole('link', { name: 'Browse the library' })).toHaveAttribute('href', '/knowledge');
});

it('opens the add form and prefills category + name from a dossier deep link (?compound=<slug>)', async () => {
  searchParamsState.compound = 'bpc-157';
  vi.mocked(apiClient.getAllKnowledgeCompounds).mockResolvedValue([knowledgeEntryFixture]);
  render(<CompoundsPage />);

  await waitFor(() => expect(screen.getByLabelText('4. Optional: Manual Search/Entry')).toHaveValue('BPC-157'));
  expect(screen.getByLabelText('1. Select a Category')).toHaveValue('Peptide');
});

it('does not open the add form when there is no dossier deep link', async () => {
  render(<CompoundsPage />);
  await screen.findByText('No Compounds Yet');
  expect(screen.queryByLabelText('4. Optional: Manual Search/Entry')).not.toBeInTheDocument();
});

it('skips the knowledge lookup for a nameless compound and shows a calm empty state', async () => {
  vi.mocked(apiClient.getCompounds).mockResolvedValue([namelessCompound]);
  render(<CompoundsPage />);

  fireEvent.click(await screen.findByText('Unnamed compound'));

  await screen.findByText('No reference entry for this compound');
  expect(apiClient.getKnowledgeEntry).not.toHaveBeenCalled();
});

it('lets the owner give a nameless compound a name via Edit, recovering the broken record', async () => {
  vi.mocked(apiClient.getCompounds).mockResolvedValue([namelessCompound]);
  vi.mocked(apiClient.getKnowledgeEntry).mockResolvedValue(knowledgeEntryFixture);
  vi.mocked(apiClient.updateCompound).mockResolvedValue({ ...namelessCompound, name: 'BPC-157' });
  render(<CompoundsPage />);

  fireEvent.click(await screen.findByText('Unnamed compound'));
  fireEvent.click(screen.getByRole('button', { name: 'Edit this compound' }));
  fireEvent.change(screen.getByLabelText('Compound name'), { target: { value: 'BPC-157' } });
  fireEvent.click(screen.getByRole('button', { name: 'Save Changes' }));

  await waitFor(() => expect(apiClient.updateCompound).toHaveBeenCalledWith(
    'profile-fixture',
    'compound-nameless',
    expect.objectContaining({ name: 'BPC-157' })
  ));
  await screen.findByRole('heading', { name: 'BPC-157' });
  await waitFor(() => expect(apiClient.getKnowledgeEntry).toHaveBeenCalledWith('BPC-157'));
});

it('cancels an edit without saving and returns focus to the Edit action', async () => {
  vi.mocked(apiClient.getCompounds).mockResolvedValue([namedCompound]);
  vi.mocked(apiClient.getKnowledgeEntry).mockResolvedValue(knowledgeEntryFixture);
  render(<CompoundsPage />);

  fireEvent.click(await screen.findByText('BPC-157'));
  fireEvent.click(screen.getByRole('button', { name: 'Edit BPC-157' }));
  fireEvent.click(screen.getByRole('button', { name: 'Cancel' }));

  expect(apiClient.updateCompound).not.toHaveBeenCalled();
  expect(screen.getByRole('button', { name: 'Edit BPC-157' })).toHaveFocus();
});

it('deletes a compound via an inline confirm (no native dialog), removing it from the list', async () => {
  const confirmSpy = vi.spyOn(window, 'confirm');
  vi.mocked(apiClient.getCompounds).mockResolvedValue([namedCompound]);
  vi.mocked(apiClient.getKnowledgeEntry).mockResolvedValue(knowledgeEntryFixture);
  vi.mocked(apiClient.deleteCompound).mockResolvedValue(undefined);
  render(<CompoundsPage />);

  fireEvent.click(await screen.findByText('BPC-157'));
  fireEvent.click(screen.getByRole('button', { name: 'Delete BPC-157' }));
  expect(screen.getByText("Delete this compound? This can't be undone.")).toBeInTheDocument();

  fireEvent.click(screen.getByRole('button', { name: 'Confirm delete' }));

  await waitFor(() => expect(apiClient.deleteCompound).toHaveBeenCalledWith('profile-fixture', 'compound-named'));
  await waitFor(() => expect(screen.queryByText('BPC-157')).not.toBeInTheDocument());
  expect(confirmSpy).not.toHaveBeenCalled();
});

it('cancels an inline delete confirmation without deleting anything', async () => {
  vi.mocked(apiClient.getCompounds).mockResolvedValue([namedCompound]);
  vi.mocked(apiClient.getKnowledgeEntry).mockResolvedValue(knowledgeEntryFixture);
  render(<CompoundsPage />);

  fireEvent.click(await screen.findByText('BPC-157'));
  fireEvent.click(screen.getByRole('button', { name: 'Delete BPC-157' }));
  fireEvent.click(screen.getByRole('button', { name: 'Cancel' }));

  expect(screen.queryByText("Delete this compound? This can't be undone.")).not.toBeInTheDocument();
  expect(apiClient.deleteCompound).not.toHaveBeenCalled();
  expect(screen.getAllByText('BPC-157').length).toBeGreaterThan(0);
  expect(screen.getByRole('button', { name: 'Delete BPC-157' })).toHaveFocus();
});

it('rolls back an optimistic delete when the server call fails', async () => {
  vi.mocked(apiClient.getCompounds).mockResolvedValue([namedCompound]);
  vi.mocked(apiClient.getKnowledgeEntry).mockResolvedValue(knowledgeEntryFixture);
  vi.mocked(apiClient.deleteCompound).mockRejectedValue(new Error('boom'));
  render(<CompoundsPage />);

  fireEvent.click(await screen.findByText('BPC-157'));
  fireEvent.click(screen.getByRole('button', { name: 'Delete BPC-157' }));
  fireEvent.click(screen.getByRole('button', { name: 'Confirm delete' }));

  await screen.findByText('Failed to delete compound');
  expect(screen.getAllByText('BPC-157').length).toBeGreaterThan(0);
  await waitFor(() => expect(screen.getByRole('button', { name: 'Delete BPC-157' })).toHaveFocus());
});

it('routes to consent onboarding when delete is blocked by consent_required', async () => {
  vi.mocked(apiClient.getCompounds).mockResolvedValue([namedCompound]);
  vi.mocked(apiClient.getKnowledgeEntry).mockResolvedValue(knowledgeEntryFixture);
  vi.mocked(apiClient.deleteCompound).mockRejectedValue(new ApiError(403, 'Consent required', { code: 'consent_required' }));
  render(<CompoundsPage />);

  fireEvent.click(await screen.findByText('BPC-157'));
  fireEvent.click(screen.getByRole('button', { name: 'Delete BPC-157' }));
  fireEvent.click(screen.getByRole('button', { name: 'Confirm delete' }));

  await waitFor(() => expect(push).toHaveBeenCalledWith('/onboarding/consent?returnTo=%2Fcompounds'));
});

it('routes to consent onboarding when an edit save is blocked by consent_required', async () => {
  vi.mocked(apiClient.getCompounds).mockResolvedValue([namedCompound]);
  vi.mocked(apiClient.getKnowledgeEntry).mockResolvedValue(knowledgeEntryFixture);
  vi.mocked(apiClient.updateCompound).mockRejectedValue(new ApiError(403, 'Consent required', { code: 'consent_required' }));
  render(<CompoundsPage />);

  fireEvent.click(await screen.findByText('BPC-157'));
  fireEvent.click(screen.getByRole('button', { name: 'Edit BPC-157' }));
  fireEvent.click(screen.getByRole('button', { name: 'Save Changes' }));

  await waitFor(() => expect(push).toHaveBeenCalledWith('/onboarding/consent?returnTo=%2Fcompounds'));
});

function deferredDelete() {
  let reject!: (error: Error) => void;
  const promise = new Promise<void>((_, fail) => { reject = fail; });
  return { promise, reject };
}
it('restores only failed deletion while preserving a newer successful deletion and selection', async () => {
  const a = { ...namedCompound, id: 'a', name: 'A' };
  const b = { ...namedCompound, id: 'b', name: 'B' };
  const c = { ...namedCompound, id: 'c', name: 'C' };
  const pending = deferredDelete();
  vi.mocked(apiClient.getCompounds).mockResolvedValue([a, b, c]);
  vi.mocked(apiClient.deleteCompound).mockImplementation((_, id) => id === 'a' ? pending.promise : Promise.resolve());
  render(<CompoundsPage />);
  fireEvent.click(await screen.findByText('A'));
  fireEvent.click(screen.getByRole('button', { name: 'Delete A' }));
  fireEvent.click(screen.getByRole('button', { name: 'Confirm delete' }));
  fireEvent.click(screen.getByText('B'));
  fireEvent.click(screen.getByRole('button', { name: 'Delete B' }));
  fireEvent.click(screen.getByRole('button', { name: 'Confirm delete' }));
  await waitFor(() => expect(apiClient.deleteCompound).toHaveBeenCalledTimes(2));
  fireEvent.click(screen.getByText('C'));
  await act(async () => pending.reject(new Error('A failed')));
  expect(screen.getByText('A')).toBeInTheDocument();
  expect(screen.queryByText('B')).not.toBeInTheDocument();
  expect(screen.getByRole('heading', { name: 'C' })).toBeInTheDocument();
});
it('does not restore a failed old-profile deletion into the new profile', async () => {
  const pending = deferredDelete();
  vi.mocked(apiClient.getCompounds).mockImplementation(async id => id === 'profile-fixture' ? [namedCompound] : [{ ...namedCompound, id: 'other', name: 'Other profile', personId: id }]);
  vi.mocked(apiClient.deleteCompound).mockReturnValue(pending.promise);
  const view = render(<CompoundsPage />);
  fireEvent.click(await screen.findByText('BPC-157'));
  fireEvent.click(screen.getByRole('button', { name: 'Delete BPC-157' }));
  fireEvent.click(screen.getByRole('button', { name: 'Confirm delete' }));
  profileState.currentProfileId = 'profile-other'; view.rerender(<CompoundsPage />);
  await screen.findByText('Other profile');
  await act(async () => pending.reject(new Error('Old profile failed')));
  expect(screen.getByText('Other profile')).toBeInTheDocument();
  expect(screen.queryByText('BPC-157')).not.toBeInTheDocument();
});

describe('detail grid reclaims width when the sidebar is collapsed', () => {
  beforeEach(() => {
    vi.mocked(apiClient.getCompounds).mockResolvedValue([
      namedCompound,
    ]);
  });

  it('uses a 2/1 (3-col) split by default', async () => {
    render(<CompoundsPage />);
    await screen.findByText('BPC-157');

    const grid = screen.getByTestId('compounds-detail-grid');
    expect(grid.className).toContain('lg:grid-cols-3');
    expect(grid.className).not.toContain('lg:grid-cols-5');
  });

  it('widens the detail column to a 2/3 (5-col) split when the sidebar is collapsed', async () => {
    window.localStorage.setItem(SIDEBAR_COLLAPSED_KEY, '1');
    render(<CompoundsPage />);
    await screen.findByText('BPC-157');

    const grid = screen.getByTestId('compounds-detail-grid');
    expect(grid.className).toContain('lg:grid-cols-5');
    expect(grid.className).not.toContain('lg:grid-cols-3');
  });
});

it('does not steal focus from newer input when an earlier deletion fails', async () => {
  const pending = deferredDelete();
  vi.mocked(apiClient.getCompounds).mockResolvedValue([namedCompound]);
  vi.mocked(apiClient.deleteCompound).mockReturnValue(pending.promise);
  render(<CompoundsPage />);
  fireEvent.click(await screen.findByText('BPC-157'));
  fireEvent.click(screen.getByRole('button', { name: 'Delete BPC-157' }));
  fireEvent.click(screen.getByRole('button', { name: 'Confirm delete' }));
  fireEvent.click(screen.getByRole('button', { name: 'Add Compound', exact: true }));
  const input = screen.getByLabelText('4. Optional: Manual Search/Entry');
  input.focus();
  fireEvent.change(input, { target: { value: 'A new record' } });
  await act(async () => pending.reject(new Error('Delayed deletion failed')));
  expect(screen.getByRole('button', { name: 'Delete BPC-157' })).toBeInTheDocument();
  expect(input).toHaveFocus();
  expect(input).toHaveValue('A new record');
});

it('does not overwrite manual category and name while dossier prefill is loading', async () => {
  searchParamsState.compound = 'bpc-157';
  vi.mocked(apiClient.createCompound).mockResolvedValue({ ...namedCompound, name: 'Creatine', category: 'Supplement' });
  let resolve!: (entries: KnowledgeEntry[]) => void;
  vi.mocked(apiClient.getAllKnowledgeCompounds).mockReturnValue(new Promise(done => { resolve = done; }));
  render(<CompoundsPage />);
  const category = await screen.findByLabelText('1. Select a Category');
  fireEvent.change(category, { target: { value: 'Supplement' } });
  const name = screen.getByLabelText('4. Optional: Manual Search/Entry');
  fireEvent.change(name, { target: { value: 'Creatine' } });
  await act(async () => resolve([knowledgeEntryFixture]));
  expect(category).toHaveValue('Supplement');
  expect(name).toHaveValue('Creatine');
  fireEvent.submit(name.closest('form')!);
  await waitFor(() => expect(apiClient.createCompound).toHaveBeenCalledWith('profile-fixture', expect.objectContaining({ name: 'Creatine', category: 'Supplement' })));
});


describe('side-panel request identity', () => {
  const a = { ...namedCompound, id: 'a', name: 'Alpha' };
  const b = { ...namedCompound, id: 'b', name: 'Beta' };
  const c = { ...namedCompound, id: 'c', name: 'Gamma' };
  const flag = (names: string[]) => [{ id: 'flag', compoundNames: names, severity: null, createdAtUtc: '2026-09-17T00:00:00Z' }];
  function deferred<T>() {
    let resolve!: (value: T) => void;
    let reject!: (reason: Error) => void;
    const promise = new Promise<T>((yes, no) => { resolve = yes; reject = no; });
    return { promise, resolve, reject };
  }
  beforeEach(() => {
    vi.mocked(apiClient.getCompounds).mockResolvedValue([a, b, c]);
    vi.mocked(apiClient.getKnowledgeEntry).mockImplementation(async name => ({ ...knowledgeEntryFixture, canonicalName: name }));
  });
  it.each(['success', 'error'])('ignores old selection %s after newer overlap and reference success', async outcome => {
    const old = deferred<Awaited<ReturnType<typeof apiClient.checkOverlap>>>();
    const oldEntry = deferred<KnowledgeEntry>();
    vi.mocked(apiClient.checkOverlap).mockReturnValueOnce(old.promise).mockResolvedValueOnce(flag(['Beta', 'Gamma']));
    vi.mocked(apiClient.getKnowledgeEntry).mockReturnValueOnce(oldEntry.promise);
    render(<CompoundsPage />);
    fireEvent.click(await screen.findByText('Alpha'));
    fireEvent.click(screen.getByText('Beta'));
    await waitFor(() => expect(screen.getByTestId('reference-entry')).toHaveTextContent('Beta'));
    await waitFor(() => expect(screen.getAllByText('Gamma')).toHaveLength(2));
    await act(async () => {
      if (outcome === 'success') { old.resolve(flag(['Alpha', 'Beta'])); oldEntry.resolve({ ...knowledgeEntryFixture, canonicalName: 'Alpha' }); }
      else { old.reject(new Error('old overlap')); oldEntry.reject(new Error('old lookup')); }
    });
    expect(screen.getAllByText('Gamma')).toHaveLength(2);
    expect(screen.getByTestId('reference-entry')).toHaveTextContent('Beta');
    expect(apiClient.checkOverlap).toHaveBeenCalledTimes(2);
  });
  it.each(['success', 'error'])('clears old profile selection and rejects its pending %s responses', async outcome => {
    const old = deferred<Awaited<ReturnType<typeof apiClient.checkOverlap>>>();
    const oldEntry = deferred<KnowledgeEntry>();
    vi.mocked(apiClient.checkOverlap).mockReturnValueOnce(old.promise);
    vi.mocked(apiClient.getKnowledgeEntry).mockReturnValueOnce(oldEntry.promise);
    const view = render(<CompoundsPage />);
    fireEvent.click(await screen.findByText('Alpha'));
    profileState.currentProfileId = 'new-profile'; view.rerender(<CompoundsPage />);
    await waitFor(() => expect(screen.getByText('Select a compound to view details')).toBeVisible());
    fireEvent.click(screen.getByText('Beta'));
    await waitFor(() => expect(screen.getByTestId('reference-entry')).toHaveTextContent('Beta'));
    await act(async () => {
      if (outcome === 'success') { old.resolve(flag(['Alpha', 'Gamma'])); oldEntry.resolve({ ...knowledgeEntryFixture, canonicalName: 'Alpha' }); }
      else { old.reject(new Error('old profile')); oldEntry.reject(new Error('old profile lookup')); }
    });
    expect(screen.queryByText('Flagged with other active compounds in this profile')).not.toBeInTheDocument();
    expect(screen.getByTestId('reference-entry')).toHaveTextContent('Beta');
  });
  it.each(['settled', 'pending'])('invalidates %s flags on rename without posting another overlap request', async state => {
    const overlap = deferred<Awaited<ReturnType<typeof apiClient.checkOverlap>>>();
    vi.mocked(apiClient.checkOverlap).mockReturnValue(state === 'pending' ? overlap.promise : Promise.resolve(flag(['Alpha', 'Beta'])));
    vi.mocked(apiClient.updateCompound).mockResolvedValue({ ...a, name: 'Renamed' });
    render(<CompoundsPage />);
    fireEvent.click(await screen.findByText('Alpha'));
    if (state === 'settled') await screen.findByText('Flagged with other active compounds in this profile');
    fireEvent.click(screen.getByRole('button', { name: 'Edit Alpha' }));
    fireEvent.change(screen.getByLabelText('Compound name'), { target: { value: 'Renamed' } });
    fireEvent.click(screen.getByRole('button', { name: 'Save Changes' }));
    await screen.findByRole('heading', { name: 'Renamed' });
    await act(async () => overlap.resolve(flag(['Alpha', 'Beta'])));
    expect(screen.queryByText('Flagged with other active compounds in this profile')).not.toBeInTheDocument();
    expect(apiClient.checkOverlap).toHaveBeenCalledTimes(1);
  });
  it('invalidates a newer panel request when a partner deletion rolls back', async () => {
    const deletion = deferred<void>();
    const overlap = deferred<Awaited<ReturnType<typeof apiClient.checkOverlap>>>();
    vi.mocked(apiClient.deleteCompound).mockReturnValue(deletion.promise);
    vi.mocked(apiClient.checkOverlap).mockResolvedValueOnce([]).mockReturnValueOnce(overlap.promise);
    render(<CompoundsPage />);
    fireEvent.click(await screen.findByText('Beta'));
    fireEvent.click(screen.getByRole('button', { name: 'Delete Beta' }));
    fireEvent.click(screen.getByRole('button', { name: 'Confirm delete' }));
    fireEvent.click(screen.getByText('Alpha'));
    await act(async () => deletion.reject(new Error('rollback')));
    await act(async () => overlap.resolve(flag(['Alpha', 'Gamma'])));
    expect(screen.queryByText('Flagged with other active compounds in this profile')).not.toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Alpha' })).toBeVisible();
    expect(screen.getByText('Beta')).toBeVisible();
    expect(apiClient.checkOverlap).toHaveBeenCalledTimes(2);
  });
  it.each(['settled', 'pending'])('clears %s flags after add without an automatic overlap POST', async state => {
    const overlap = deferred<Awaited<ReturnType<typeof apiClient.checkOverlap>>>();
    const reference = deferred<KnowledgeEntry>();
    vi.mocked(apiClient.getKnowledgeEntry).mockReturnValueOnce(reference.promise);
    vi.mocked(apiClient.checkOverlap).mockReturnValue(state === 'pending' ? overlap.promise : Promise.resolve(flag(['Alpha', 'Beta'])));
    vi.mocked(apiClient.createCompound).mockImplementation(async (_, data) => ({ ...data, id: 'added' }));
    render(<CompoundsPage />);
    fireEvent.click(await screen.findByText('Alpha'));
    if (state === 'settled') await screen.findByText('Flagged with other active compounds in this profile');
    fireEvent.click(screen.getByRole('button', { name: 'Add Compound', exact: true }));
    fireEvent.change(screen.getByLabelText('1. Select a Category'), { target: { value: 'Peptide' } });
    fireEvent.change(screen.getByLabelText('4. Optional: Manual Search/Entry'), { target: { value: 'Added' } });
    fireEvent.click(screen.getAllByRole('button', { name: 'Add Compound', exact: true }).at(-1)!);
    await screen.findByText('Added');
    await act(async () => { overlap.resolve(flag(['Alpha', 'Beta'])); reference.resolve({ ...knowledgeEntryFixture, canonicalName: 'Alpha' }); });
    expect(screen.getByTestId('reference-entry')).toHaveTextContent('Alpha');
    expect(screen.queryByText('Flagged with other active compounds in this profile')).not.toBeInTheDocument();
    expect(apiClient.checkOverlap).toHaveBeenCalledTimes(1);
  });
  it('does not let an old profile list restore membership after switching profiles', async () => {
    const oldList = deferred<CompoundRecord[]>();
    vi.mocked(apiClient.getCompounds).mockReturnValueOnce(oldList.promise).mockResolvedValueOnce([b, c]);
    const view = render(<CompoundsPage />);
    profileState.currentProfileId = 'new-profile'; view.rerender(<CompoundsPage />);
    fireEvent.click(await screen.findByText('Beta'));
    await act(async () => oldList.resolve([a]));
    expect(screen.queryByText('Alpha')).not.toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Beta' })).toBeVisible();
    expect(apiClient.checkOverlap).toHaveBeenCalledTimes(1);
  });
  it('keeps a newer selection when a previous record rename completes', async () => {
    const edit = deferred<CompoundRecord>();
    vi.mocked(apiClient.updateCompound).mockReturnValue(edit.promise);
    vi.mocked(apiClient.checkOverlap).mockResolvedValue(flag(['Beta', 'Gamma']));
    render(<CompoundsPage />);
    fireEvent.click(await screen.findByText('Alpha'));
    fireEvent.click(screen.getByRole('button', { name: 'Edit Alpha' }));
    fireEvent.change(screen.getByLabelText('Compound name'), { target: { value: 'Renamed' } });
    fireEvent.click(screen.getByRole('button', { name: 'Save Changes' }));
    fireEvent.click(screen.getByText('Beta'));
    await screen.findByText('Flagged with other active compounds in this profile');
    await act(async () => edit.resolve({ ...a, name: 'Renamed' }));
    expect(screen.getByRole('heading', { name: 'Beta' })).toBeVisible();
    expect(screen.getByText('Renamed')).toBeVisible();
    expect(screen.queryByText('Flagged with other active compounds in this profile')).not.toBeInTheDocument();
    expect(apiClient.checkOverlap).toHaveBeenCalledTimes(2);
  });

  describe('edit completion identity', () => {
    it.each(['failure', 'consent'])('ignores old %s after a newer selection opens its edit form', async kind => {
      const old = deferred<CompoundRecord>();
      vi.mocked(apiClient.updateCompound).mockReturnValue(old.promise);
      render(<CompoundsPage />);
      fireEvent.click(await screen.findByText('Alpha'));
      fireEvent.click(screen.getByRole('button', { name: 'Edit Alpha' }));
      fireEvent.click(screen.getByRole('button', { name: 'Save Changes' }));
      fireEvent.click(screen.getByText('Beta'));
      fireEvent.click(screen.getByRole('button', { name: 'Edit Beta' }));
      await act(async () => old.reject(kind === 'consent' ? new ApiError(403, 'Old consent', { code: 'consent_required' }) : new Error('Old edit failed')));
      expect(screen.queryByRole('alert')).not.toBeInTheDocument();
      expect(push).not.toHaveBeenCalled();
      expect(screen.getByLabelText('Compound name')).toHaveValue('Beta');
      expect(screen.getByRole('button', { name: 'Save Changes' })).toBeEnabled();
    });
    it('leaves a newer save pending when an old save rejects and preserves current retry errors', async () => {
      vi.spyOn(console, 'error').mockImplementation(() => {});
      const old = deferred<CompoundRecord>();
      const current = deferred<CompoundRecord>();
      vi.mocked(apiClient.updateCompound).mockReturnValueOnce(old.promise).mockReturnValueOnce(current.promise);
      render(<CompoundsPage />);
      fireEvent.click(await screen.findByText('Alpha'));
      fireEvent.click(screen.getByRole('button', { name: 'Edit Alpha' }));
      fireEvent.click(screen.getByRole('button', { name: 'Save Changes' }));
      fireEvent.click(screen.getByText('Beta'));
      fireEvent.click(screen.getByRole('button', { name: 'Edit Beta' }));
      expect(screen.getByRole('button', { name: 'Save Changes' })).toBeEnabled();
      fireEvent.click(screen.getByRole('button', { name: 'Save Changes' }));
      expect(apiClient.updateCompound).toHaveBeenCalledTimes(2);
      await act(async () => old.reject(new Error('Old failed')));
      expect(screen.getByRole('button', { name: 'Saving...' })).toBeDisabled();
      expect(screen.queryByRole('alert')).not.toBeInTheDocument();
      await act(async () => current.reject(new ApiError(500, 'Current edit failed')));
      expect(screen.getByRole('alert')).toHaveTextContent('Current edit failed');
      expect(screen.getByRole('button', { name: 'Save Changes' })).toBeEnabled();
      expect(screen.getByLabelText('Compound name')).toHaveValue('Beta');
      vi.restoreAllMocks();
    });
    it('does not redirect after old-profile edit consent rejection', async () => {
      const old = deferred<CompoundRecord>();
      vi.mocked(apiClient.updateCompound).mockReturnValue(old.promise);
      const view = render(<CompoundsPage />);
      fireEvent.click(await screen.findByText('Alpha'));
      fireEvent.click(screen.getByRole('button', { name: 'Edit Alpha' }));
      fireEvent.click(screen.getByRole('button', { name: 'Save Changes' }));
      profileState.currentProfileId = 'new-profile'; view.rerender(<CompoundsPage />);
      fireEvent.click(await screen.findByText('Beta'));
      await act(async () => old.reject(new ApiError(403, 'Old consent', { code: 'consent_required' })));
      expect(push).not.toHaveBeenCalled();
      expect(screen.getByRole('heading', { name: 'Beta' })).toBeVisible();
    });
  });

});
