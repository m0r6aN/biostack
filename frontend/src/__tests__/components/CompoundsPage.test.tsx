import { SIDEBAR_COLLAPSED_KEY } from '@/lib/sidebarCollapse';
import CompoundsPage from '@/app/compounds/page';
import { ApiError, apiClient } from '@/lib/api';
import type { CompoundRecord, KnowledgeEntry } from '@/lib/types';
import { act, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
const profileState = vi.hoisted(() => ({ currentProfileId: 'profile-fixture' }));
vi.mock('@/lib/context', () => ({ useProfile: () => profileState }));
const { push } = vi.hoisted(() => ({ push: vi.fn() }));
vi.mock('next/navigation', () => ({ useRouter: () => ({ push }) }));
vi.mock('@/components/Header', () => ({ Header: ({ actions }: { actions?: React.ReactNode }) => <header>{actions}</header> }));
vi.mock('@/components/ActiveProfileChip', () => ({ ActiveProfileChip: () => null }));
vi.mock('@/components/knowledge/CompoundIntelligenceCard', () => ({ CompoundIntelligenceCard: () => null }));
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
  vi.clearAllMocks();
  profileState.currentProfileId = 'profile-fixture';
  window.localStorage.clear();
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
