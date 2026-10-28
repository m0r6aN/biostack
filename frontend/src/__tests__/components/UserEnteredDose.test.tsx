import CompoundsPage from '@/app/compounds/page';
import { DashboardTab } from '@/components/protocol-portal/tabs/DashboardTab';
import { DayDetailModal } from '@/components/protocol-portal/DayDetailModal';
import { ProviderObservationalSummary } from '@/components/protocols/ProviderObservationalSummary';
import { ApiClient, ApiError, apiClient } from '@/lib/api';
import type { CompoundRecord, DaySchedule, KnowledgeEntry } from '@/lib/types';
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { makeSavedProviderSummaryProtocol, providerSummaryGeneratedAt } from '../fixtures/providerSummary';

const profileState = vi.hoisted(() => ({ currentProfileId: 'profile-fixture' }));
vi.mock('@/lib/context', () => ({ useProfile: () => profileState }));
const searchParamsState = vi.hoisted(() => ({ compound: null as string | null }));
vi.mock('next/navigation', () => ({
  useRouter: () => ({ push: vi.fn() }),
  useSearchParams: () => ({ get: (key: string) => (key === 'compound' ? searchParamsState.compound : null) }),
}));
vi.mock('@/components/Header', () => ({ Header: ({ actions }: { actions?: React.ReactNode }) => <header>{actions}</header> }));
vi.mock('@/components/ActiveProfileChip', () => ({ ActiveProfileChip: () => null }));
vi.mock('@/components/knowledge/CompoundIntelligenceCard', () => ({
  CompoundIntelligenceCard: ({ entry }: { entry: KnowledgeEntry }) => <div>{entry.canonicalName}</div>,
}));
vi.mock('@/lib/api', async (importOriginal) => ({
  ...(await importOriginal<typeof import('@/lib/api')>()),
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

const DOSE_LABELS = ['Dose amount', 'Dose unit', 'Dose frequency', 'Dose time or schedule'] as const;

const knowledgeEntry = (name: string): KnowledgeEntry =>
  ({
    canonicalName: name,
    aliases: [],
    classification: 'Peptide',
    // The library knows a "recommended" dose; the form must never use it.
    recommendedDosage: '500 mcg daily',
    standardDosageRange: '250-500 mcg',
    maxReportedDose: '1000 mcg',
    frequency: 'daily',
    benefits: [],
  }) as unknown as KnowledgeEntry;

async function openAddForm() {
  render(<CompoundsPage />);
  await screen.findByText('No Compounds Yet');
  fireEvent.click(screen.getByRole('button', { name: 'Add Compound', exact: true }));
}

function fillBasics(name = 'BPC-157') {
  fireEvent.change(screen.getByLabelText('1. Select a Category'), { target: { value: 'Peptide' } });
  fireEvent.change(screen.getByLabelText('4. Optional: Manual Search/Entry'), { target: { value: name } });
}

function submitForm() {
  fireEvent.click(screen.getAllByRole('button', { name: 'Add Compound', exact: true }).at(-1)!);
}

beforeEach(() => {
  vi.resetAllMocks();
  profileState.currentProfileId = 'profile-fixture';
  searchParamsState.compound = null;
  window.localStorage.clear();
  vi.mocked(apiClient.checkOverlap).mockResolvedValue([]);
  vi.mocked(apiClient.getCompounds).mockResolvedValue([]);
  vi.mocked(apiClient.getAllKnowledgeCompounds).mockResolvedValue([]);
  vi.mocked(apiClient.getKnowledgeEntry).mockRejectedValue(new Error('not in library'));
});

describe('compound form: user-entered dose', () => {
  it('starts with every dose input blank', async () => {
    await openAddForm();

    for (const label of DOSE_LABELS) {
      expect((screen.getByLabelText(label) as HTMLInputElement | HTMLSelectElement).value).toBe('');
    }
    expect(screen.getByRole('group', { name: 'Your dose (optional)' })).toBeInTheDocument();
  });

  it('offers exactly the allowed units, with a blank first option', async () => {
    await openAddForm();

    const options = within(screen.getByLabelText('Dose unit')).getAllByRole('option').map((option) => option.textContent);
    expect(options).toEqual(['Unit', 'mcg', 'mg', 'g', 'IU', 'mL', 'units']);
  });

  it('never prefills the dose from the library, even for a deep-linked compound that has a published dosage', async () => {
    searchParamsState.compound = 'bpc-157';
    vi.mocked(apiClient.getAllKnowledgeCompounds).mockResolvedValue([knowledgeEntry('BPC-157')]);
    render(<CompoundsPage />);

    await waitFor(() => expect((screen.getByLabelText('4. Optional: Manual Search/Entry') as HTMLInputElement).value).toBe('BPC-157'));
    for (const label of DOSE_LABELS) {
      expect((screen.getByLabelText(label) as HTMLInputElement | HTMLSelectElement).value).toBe('');
    }
  });

  it('enters "250 mcg, twice weekly", saves it, and shows it on the compound', async () => {
    vi.mocked(apiClient.createCompound).mockImplementation(async (_profileId, data) => ({ ...data, id: 'created-1' }) as CompoundRecord);
    await openAddForm();
    fillBasics();
    fireEvent.change(screen.getByLabelText('Dose amount'), { target: { value: '250' } });
    fireEvent.change(screen.getByLabelText('Dose unit'), { target: { value: 'mcg' } });
    fireEvent.change(screen.getByLabelText('Dose frequency'), { target: { value: 'twice weekly' } });
    fireEvent.change(screen.getByLabelText('Notes'), { target: { value: 'my own note' } });

    submitForm();

    await waitFor(() => expect(apiClient.createCompound).toHaveBeenCalledTimes(1));
    expect(apiClient.createCompound).toHaveBeenCalledWith(
      'profile-fixture',
      expect.objectContaining({
        name: 'BPC-157',
        doseAmount: 250,
        doseUnit: 'mcg',
        doseFrequency: 'twice weekly',
        doseTime: null,
        notes: 'my own note',
      }),
    );

    // On the compound list and in the compound detail.
    const row = (await screen.findByText('Your dose: 250 mcg · twice weekly')).closest('div[class*="cursor-pointer"]') as HTMLElement;
    fireEvent.click(within(row).getByText('BPC-157'));
    expect(await screen.findByText('250 mcg · twice weekly')).toBeInTheDocument();
    expect(screen.getAllByText('my own note').length).toBeGreaterThanOrEqual(1);
  });

  it('saves nothing for the dose when the user leaves it blank', async () => {
    vi.mocked(apiClient.createCompound).mockImplementation(async (_profileId, data) => ({ ...data, id: 'created-2' }) as CompoundRecord);
    await openAddForm();
    fillBasics('Creatine');

    submitForm();

    await waitFor(() => expect(apiClient.createCompound).toHaveBeenCalledTimes(1));
    expect(apiClient.createCompound).toHaveBeenCalledWith(
      'profile-fixture',
      expect.objectContaining({ doseAmount: null, doseUnit: null, doseFrequency: null, doseTime: null }),
    );
    expect(screen.queryByText(/Your dose:/)).not.toBeInTheDocument();
  });

  it.each([
    ['0', 'mg', 'Amount must be a number greater than zero.'],
    ['-3', 'mg', 'Amount must be a number greater than zero.'],
    ['250', '', 'Choose a unit for the amount.'],
  ])('blocks saving amount %s / unit "%s" with a plain error and no dose advice', async (amount, unit, message) => {
    await openAddForm();
    fillBasics();
    fireEvent.change(screen.getByLabelText('Dose amount'), { target: { value: amount } });
    fireEvent.change(screen.getByLabelText('Dose unit'), { target: { value: unit } });

    submitForm();

    expect(await screen.findByText(message)).toBeInTheDocument();
    expect(apiClient.createCompound).not.toHaveBeenCalled();
    // Nothing implies a recommended or typical dose.
    expect(document.body.textContent?.toLowerCase()).not.toMatch(/recommended dose|typical dose|safe dose|too high|too low|normal range/);
  });

  it('keeps the entered dose in the form when saving fails, so the user can retry', async () => {
    vi.spyOn(console, 'error').mockImplementation(() => {});
    vi.mocked(apiClient.createCompound).mockRejectedValueOnce(new Error('network'));
    await openAddForm();
    fillBasics();
    fireEvent.change(screen.getByLabelText('Dose amount'), { target: { value: '250' } });
    fireEvent.change(screen.getByLabelText('Dose unit'), { target: { value: 'mcg' } });

    submitForm();

    await waitFor(() => expect(apiClient.createCompound).toHaveBeenCalledTimes(1));
    expect((screen.getByLabelText('Dose amount') as HTMLInputElement).value).toBe('250');
    expect((screen.getByLabelText('Dose unit') as HTMLSelectElement).value).toBe('mcg');
  });
});

describe('compound edit: user-entered dose', () => {
  const withDose: CompoundRecord = {
    id: 'compound-dose',
    personId: 'profile-fixture',
    name: 'BPC-157',
    category: 'Peptide',
    startDate: '2026-09-01T00:00:00Z',
    endDate: null,
    status: 'Active',
    notes: 'keep me',
    sourceType: 'Manual',
    doseAmount: 250,
    doseUnit: 'mcg',
    doseFrequency: 'twice weekly',
    doseTime: 'mornings',
  };

  async function openEdit(compound: CompoundRecord) {
    vi.mocked(apiClient.getCompounds).mockResolvedValue([compound]);
    render(<CompoundsPage />);
    fireEvent.click(await screen.findByText(compound.name));
    fireEvent.click(await screen.findByRole('button', { name: `Edit ${compound.name}` }));
  }

  it('loads the saved dose into the edit form, then saves changes and keeps Notes', async () => {
    vi.mocked(apiClient.updateCompound).mockImplementation(async (_p, id, data) => ({ ...data, id }) as CompoundRecord);
    await openEdit(withDose);

    expect((screen.getByLabelText('Dose amount') as HTMLInputElement).value).toBe('250');
    expect((screen.getByLabelText('Dose unit') as HTMLSelectElement).value).toBe('mcg');
    expect((screen.getByLabelText('Dose frequency') as HTMLInputElement).value).toBe('twice weekly');
    expect((screen.getByLabelText('Dose time or schedule') as HTMLInputElement).value).toBe('mornings');

    fireEvent.change(screen.getByLabelText('Dose amount'), { target: { value: '300' } });
    fireEvent.click(screen.getByRole('button', { name: 'Save Changes' }));

    await waitFor(() => expect(apiClient.updateCompound).toHaveBeenCalledTimes(1));
    expect(apiClient.updateCompound).toHaveBeenCalledWith(
      'profile-fixture',
      'compound-dose',
      expect.objectContaining({ doseAmount: 300, doseUnit: 'mcg', doseFrequency: 'twice weekly', doseTime: 'mornings', notes: 'keep me' }),
    );
  });

  it('clears the dose when the user empties the fields', async () => {
    vi.mocked(apiClient.updateCompound).mockImplementation(async (_p, id, data) => ({ ...data, id }) as CompoundRecord);
    await openEdit(withDose);

    fireEvent.change(screen.getByLabelText('Dose amount'), { target: { value: '' } });
    fireEvent.change(screen.getByLabelText('Dose unit'), { target: { value: '' } });
    fireEvent.change(screen.getByLabelText('Dose frequency'), { target: { value: '' } });
    fireEvent.change(screen.getByLabelText('Dose time or schedule'), { target: { value: '' } });
    fireEvent.click(screen.getByRole('button', { name: 'Save Changes' }));

    await waitFor(() => expect(apiClient.updateCompound).toHaveBeenCalledTimes(1));
    expect(apiClient.updateCompound).toHaveBeenCalledWith(
      'profile-fixture',
      'compound-dose',
      expect.objectContaining({ doseAmount: null, doseUnit: null, doseFrequency: null, doseTime: null }),
    );
  });

  it('leaves the dose blank for an older record that never had one (the dose is not guessed from Notes)', async () => {
    await openEdit({ ...withDose, notes: '250 mcg twice weekly', doseAmount: null, doseUnit: null, doseFrequency: null, doseTime: null });

    for (const label of DOSE_LABELS) {
      expect((screen.getByLabelText(label) as HTMLInputElement | HTMLSelectElement).value).toBe('');
    }
  });
});

describe('schedule and provider summary: user-entered dose', () => {
  const today: DaySchedule = {
    dateIso: '2026-10-28',
    title: 'Today',
    subtitle: 'Wednesday',
    items: [
      { time: 'mornings', name: 'BPC-157', detail: 'my own note', dose: '250 mcg · twice weekly', status: 'upcoming' },
      { time: 'As scheduled', name: 'Creatine', detail: 'Supplement', dose: null, status: 'upcoming' },
    ],
  };

  it('shows the dose on the My Protocol schedule, and only for compounds that have one', () => {
    render(<DashboardTab today={today} onViewCalendar={vi.fn()} onViewLabs={vi.fn()} onLogDoses={vi.fn()} onSaveCareTeamNote={vi.fn()} />);

    expect(screen.getByText('Your dose: 250 mcg · twice weekly')).toBeInTheDocument();
    expect(screen.getAllByText(/Your dose:/)).toHaveLength(1);
    expect(screen.getByText('mornings')).toBeInTheDocument();
  });

  it('shows the dose in the day detail view', () => {
    render(<DayDetailModal day={today} onClose={vi.fn()} />);

    expect(screen.getByText('Your dose: 250 mcg · twice weekly')).toBeInTheDocument();
  });

  it('labels the dose as user-entered in the provider summary, on screen and in the copied text', async () => {
    const protocol = makeSavedProviderSummaryProtocol();
    protocol.items[0].compound = {
      ...protocol.items[0].compound!,
      doseAmount: 250,
      doseUnit: 'mcg',
      doseFrequency: 'twice weekly',
      doseTime: 'mornings',
    };
    const writeText = vi.fn().mockResolvedValue(undefined);
    Object.defineProperty(navigator, 'clipboard', { value: { writeText }, configurable: true });
    render(<ProviderObservationalSummary protocol={protocol} generatedAt={providerSummaryGeneratedAt} />);

    expect(screen.getByText('User-entered dose')).toBeInTheDocument();
    expect(screen.getByText('250 mcg · twice weekly · mornings')).toBeInTheDocument();

    await userEvent.click(screen.getByRole('button', { name: 'Copy summary' }));
    expect(writeText).toHaveBeenCalledWith(expect.stringContaining('User-entered dose: 250 mcg · twice weekly · mornings'));
  });

  it('says "Not recorded" in the provider summary when the user entered no dose', () => {
    render(<ProviderObservationalSummary protocol={makeSavedProviderSummaryProtocol()} generatedAt={providerSummaryGeneratedAt} />);

    const label = screen.getByText('User-entered dose');
    expect(label.parentElement).toHaveTextContent('Not recorded in this saved protocol snapshot.');
  });
});

describe('API validation errors', () => {
  it('surfaces the first field message from a validation problem response', async () => {
    const fetchMock = vi.fn().mockResolvedValue({
      ok: false,
      status: 400,
      statusText: 'Bad Request',
      json: async () => ({ title: 'One or more validation errors occurred.', errors: { doseAmount: ['Amount must be a number greater than zero.'] } }),
    });
    vi.stubGlobal('fetch', fetchMock);
    const client = new ApiClient('https://api.example.test');

    const error = await client
      .createCompound('p1', { personId: 'p1', name: 'x', category: 'Peptide', startDate: '', endDate: null, status: 'Active', notes: '', sourceType: 'Manual' })
      .catch((e: unknown) => e);

    expect(error).toBeInstanceOf(ApiError);
    expect((error as ApiError).message).toBe('Amount must be a number greater than zero.');
    vi.unstubAllGlobals();
  });
});
