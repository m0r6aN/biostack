import CompoundsPage from '@/app/compounds/page';
import { DayDetailModal } from '@/components/protocol-portal/DayDetailModal';
import { DashboardTab } from '@/components/protocol-portal/tabs/DashboardTab';
import { ProviderObservationalSummary } from '@/components/protocols/ProviderObservationalSummary';
import { apiClient } from '@/lib/api';
import type { CompoundRecord, DaySchedule, DoseLogEntry, KnowledgeEntry, ScheduleItem } from '@/lib/types';
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { makeSavedProviderSummaryProtocol, providerSummaryGeneratedAt } from '../fixtures/providerSummary';

const profileState = vi.hoisted(() => ({ currentProfileId: 'profile-fixture' }));
vi.mock('@/lib/context', () => ({ useProfile: () => profileState }));
vi.mock('next/navigation', () => ({
  useRouter: () => ({ push: vi.fn() }),
  useSearchParams: () => ({ get: () => null }),
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

beforeEach(() => {
  vi.resetAllMocks();
  profileState.currentProfileId = 'profile-fixture';
  window.localStorage.clear();
  vi.mocked(apiClient.checkOverlap).mockResolvedValue([]);
  vi.mocked(apiClient.getCompounds).mockResolvedValue([]);
  vi.mocked(apiClient.getAllKnowledgeCompounds).mockResolvedValue([]);
  vi.mocked(apiClient.getKnowledgeEntry).mockRejectedValue(new Error('not in library'));
});

async function openAddForm() {
  render(<CompoundsPage />);
  await screen.findByText('No Compounds Yet');
  fireEvent.click(screen.getByRole('button', { name: 'Add Compound' }));
}

function fillBasics(name = 'BPC-157') {
  fireEvent.change(screen.getByLabelText('1. Select a Category'), { target: { value: 'Peptide' } });
  fireEvent.change(screen.getByLabelText('4. Optional: Manual Search/Entry'), { target: { value: name } });
}

function submitForm() {
  fireEvent.click(screen.getAllByRole('button', { name: 'Add Compound' }).at(-1)!);
}

describe('compound form: structured frequency', () => {
  it('starts blank, with no kind chosen and none of the detail inputs showing', async () => {
    await openAddForm();

    expect(screen.getByRole('group', { name: 'Your schedule (optional)' })).toBeInTheDocument();
    expect((screen.getByLabelText('How often') as HTMLSelectElement).value).toBe('');
    expect(within(screen.getByLabelText('How often')).getAllByRole('option').map((o) => o.textContent)).toEqual([
      'Choose how often',
      'Every day',
      'Every N days',
      'Specific weekdays',
      'N times per week',
      'As needed',
    ]);
    expect(screen.queryByRole('checkbox', { name: 'Monday' })).not.toBeInTheDocument();
    expect(screen.queryByLabelText('Every how many days')).not.toBeInTheDocument();
    expect(screen.queryByLabelText('Times per week')).not.toBeInTheDocument();
  });

  it('is not chosen for the user when they type free text such as "twice weekly"', async () => {
    await openAddForm();

    fireEvent.change(screen.getByLabelText('Dose frequency'), { target: { value: 'twice weekly' } });

    expect((screen.getByLabelText('How often') as HTMLSelectElement).value).toBe('');
    expect(screen.queryByRole('checkbox', { name: 'Monday' })).not.toBeInTheDocument();
    // The prompt appears, but nothing is selected or suggested.
    expect(screen.getByRole('status')).toHaveTextContent('You described this as “twice weekly”');
    expect(screen.getByRole('status')).toHaveTextContent('Pick how often');
  });

  it('saves 250 mcg twice weekly on Monday and Thursday (user-chosen days)', async () => {
    vi.mocked(apiClient.createCompound).mockImplementation(async (_profileId, data) => ({ ...data, id: 'created-1' }) as CompoundRecord);
    await openAddForm();
    fillBasics();
    fireEvent.change(screen.getByLabelText('Dose amount'), { target: { value: '250' } });
    fireEvent.change(screen.getByLabelText('Dose unit'), { target: { value: 'mcg' } });
    fireEvent.change(screen.getByLabelText('Dose frequency'), { target: { value: 'twice weekly' } });
    fireEvent.change(screen.getByLabelText('How often'), { target: { value: 'weekdays' } });

    // Nothing is pre-ticked.
    for (const day of ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday']) {
      expect(screen.getByRole('checkbox', { name: day })).not.toBeChecked();
    }
    fireEvent.click(screen.getByRole('checkbox', { name: 'Thursday' }));
    fireEvent.click(screen.getByRole('checkbox', { name: 'Monday' }));

    submitForm();

    await waitFor(() => expect(apiClient.createCompound).toHaveBeenCalledTimes(1));
    expect(apiClient.createCompound).toHaveBeenCalledWith(
      'profile-fixture',
      expect.objectContaining({
        doseAmount: 250,
        doseUnit: 'mcg',
        doseFrequency: 'twice weekly',
        frequencyKind: 'weekdays',
        frequencyWeekdays: [1, 4],
        frequencyIntervalDays: null,
        frequencyTimesPerWeek: null,
        frequencyStartDate: null,
        frequencyTimesOfDay: null,
      }),
    );
    expect(await screen.findByText('Your schedule: Mon, Thu')).toBeInTheDocument();
  });

  it('saves nothing for the frequency when the user leaves it blank', async () => {
    vi.mocked(apiClient.createCompound).mockImplementation(async (_profileId, data) => ({ ...data, id: 'created-2' }) as CompoundRecord);
    await openAddForm();
    fillBasics('Creatine');

    submitForm();

    await waitFor(() => expect(apiClient.createCompound).toHaveBeenCalledTimes(1));
    expect(apiClient.createCompound).toHaveBeenCalledWith(
      'profile-fixture',
      expect.objectContaining({
        frequencyKind: null,
        frequencyIntervalDays: null,
        frequencyWeekdays: null,
        frequencyTimesPerWeek: null,
        frequencyStartDate: null,
        frequencyTimesOfDay: null,
      }),
    );
  });

  it('blocks saving specific weekdays with no day chosen', async () => {
    await openAddForm();
    fillBasics();
    fireEvent.change(screen.getByLabelText('How often'), { target: { value: 'weekdays' } });

    submitForm();

    expect(await screen.findByText('Choose at least one weekday.')).toBeInTheDocument();
    expect(apiClient.createCompound).not.toHaveBeenCalled();
  });

  it('blocks an every-N-days interval that is not a whole number from 2 to 365', async () => {
    await openAddForm();
    fillBasics();
    fireEvent.change(screen.getByLabelText('How often'), { target: { value: 'every_n_days' } });
    fireEvent.change(screen.getByLabelText('Every how many days'), { target: { value: '1' } });

    submitForm();

    expect(await screen.findByText(/Enter a whole number of days from 2 to 365/)).toBeInTheDocument();
    expect(apiClient.createCompound).not.toHaveBeenCalled();
  });

  it('saves every 3 days from a start date, with times of day', async () => {
    vi.mocked(apiClient.createCompound).mockImplementation(async (_profileId, data) => ({ ...data, id: 'created-3' }) as CompoundRecord);
    await openAddForm();
    fillBasics();
    fireEvent.change(screen.getByLabelText('How often'), { target: { value: 'every_n_days' } });
    fireEvent.change(screen.getByLabelText('Every how many days'), { target: { value: '3' } });
    fireEvent.change(screen.getByLabelText('Schedule start date'), { target: { value: '2026-10-05' } });
    fireEvent.click(screen.getByRole('button', { name: 'Add a time of day' }));
    fireEvent.change(screen.getByLabelText('Time of day 1'), { target: { value: '08:00' } });
    fireEvent.click(screen.getByRole('button', { name: 'Add a time of day' }));
    fireEvent.change(screen.getByLabelText('Time of day 2'), { target: { value: '20:00' } });

    submitForm();

    await waitFor(() => expect(apiClient.createCompound).toHaveBeenCalledTimes(1));
    expect(apiClient.createCompound).toHaveBeenCalledWith(
      'profile-fixture',
      expect.objectContaining({
        frequencyKind: 'every_n_days',
        frequencyIntervalDays: 3,
        frequencyStartDate: '2026-10-05T00:00:00Z',
        frequencyTimesOfDay: ['08:00', '20:00'],
      }),
    );
  });

  it('lets the user remove a time of day', async () => {
    await openAddForm();
    fireEvent.change(screen.getByLabelText('How often'), { target: { value: 'daily' } });
    fireEvent.click(screen.getByRole('button', { name: 'Add a time of day' }));
    expect(screen.getByLabelText('Time of day 1')).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Remove time of day 1' }));

    expect(screen.queryByLabelText('Time of day 1')).not.toBeInTheDocument();
  });

  it('describes N times per week as a weekly target, and hides the start date and times for "as needed"', async () => {
    await openAddForm();

    fireEvent.change(screen.getByLabelText('How often'), { target: { value: 'times_per_week' } });
    expect(screen.getByLabelText('Times per week')).toBeInTheDocument();
    expect(screen.getByText(/shown as a weekly target/)).toBeInTheDocument();

    fireEvent.change(screen.getByLabelText('How often'), { target: { value: 'as_needed' } });
    expect(screen.queryByLabelText('Schedule start date')).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Add a time of day' })).not.toBeInTheDocument();
  });
});

describe('compound edit: structured frequency', () => {
  const legacy: CompoundRecord = {
    id: 'compound-legacy',
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
    doseTime: null,
  };

  async function openEdit(compound: CompoundRecord) {
    vi.mocked(apiClient.getCompounds).mockResolvedValue([compound]);
    render(<CompoundsPage />);
    fireEvent.click(await screen.findByText(compound.name));
    fireEvent.click(await screen.findByRole('button', { name: `Edit ${compound.name}` }));
  }

  it('prompts an older free-text-only entry to pick a frequency, leaving it blank and keeping the text', async () => {
    vi.mocked(apiClient.updateCompound).mockImplementation(async (_p, id, data) => ({ ...data, id }) as CompoundRecord);
    await openEdit(legacy);

    expect(screen.getByRole('status')).toHaveTextContent('You described this as “twice weekly”');
    expect((screen.getByLabelText('How often') as HTMLSelectElement).value).toBe('');
    expect((screen.getByLabelText('Dose frequency') as HTMLInputElement).value).toBe('twice weekly');

    fireEvent.click(screen.getByRole('button', { name: 'Save Changes' }));

    await waitFor(() => expect(apiClient.updateCompound).toHaveBeenCalledTimes(1));
    expect(apiClient.updateCompound).toHaveBeenCalledWith(
      'profile-fixture',
      'compound-legacy',
      expect.objectContaining({ doseFrequency: 'twice weekly', frequencyKind: null, frequencyWeekdays: null }),
    );
  });

  it('stops prompting once the user picks a frequency, and saves it', async () => {
    vi.mocked(apiClient.updateCompound).mockImplementation(async (_p, id, data) => ({ ...data, id }) as CompoundRecord);
    await openEdit(legacy);

    fireEvent.change(screen.getByLabelText('How often'), { target: { value: 'weekdays' } });
    fireEvent.click(screen.getByRole('checkbox', { name: 'Monday' }));
    fireEvent.click(screen.getByRole('checkbox', { name: 'Thursday' }));

    expect(screen.queryByRole('status')).not.toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Save Changes' }));

    await waitFor(() => expect(apiClient.updateCompound).toHaveBeenCalledTimes(1));
    expect(apiClient.updateCompound).toHaveBeenCalledWith(
      'profile-fixture',
      'compound-legacy',
      expect.objectContaining({ frequencyKind: 'weekdays', frequencyWeekdays: [1, 4], doseFrequency: 'twice weekly', doseAmount: 250 }),
    );
  });

  it('loads a saved structured frequency, with no prompt', async () => {
    await openEdit({ ...legacy, frequencyKind: 'weekdays', frequencyWeekdays: [1, 4], frequencyTimesOfDay: ['08:00'] });

    expect(screen.queryByRole('status')).not.toBeInTheDocument();
    expect((screen.getByLabelText('How often') as HTMLSelectElement).value).toBe('weekdays');
    expect(screen.getByRole('checkbox', { name: 'Monday' })).toBeChecked();
    expect(screen.getByRole('checkbox', { name: 'Thursday' })).toBeChecked();
    expect(screen.getByRole('checkbox', { name: 'Tuesday' })).not.toBeChecked();
    expect((screen.getByLabelText('Time of day 1') as HTMLInputElement).value).toBe('08:00');
  });
});

// ── schedule: logging the amount actually taken ───────────────────────────

const logged = (overrides: Partial<DoseLogEntry> = {}): DoseLogEntry => ({
  id: 'entry-1',
  compoundId: 'compound-1',
  compoundName: 'BPC-157',
  logDate: '2026-10-05',
  takenAtUtc: '2026-10-05T14:00:00Z',
  amount: 250,
  unit: 'mcg',
  note: null,
  ...overrides,
});

const bpc: ScheduleItem = {
  time: 'As scheduled',
  name: 'BPC-157',
  detail: 'Peptide',
  dose: '250 mcg · twice weekly',
  status: 'upcoming',
  compoundId: 'compound-1',
  doseAmount: 250,
  doseUnit: 'mcg',
  cadence: 'Mon, Thu',
  due: 'scheduled',
  logged: [],
};

const day = (items: ScheduleItem[]): DaySchedule => ({ dateIso: '2026-10-05', title: 'Today', subtitle: 'Monday', items });

function renderDashboard(items: ScheduleItem[], extra: Partial<React.ComponentProps<typeof DashboardTab>> = {}) {
  const props = {
    today: day(items),
    onViewCalendar: vi.fn(),
    onViewLabs: vi.fn(),
    onLogDoses: vi.fn(),
    onSaveCareTeamNote: vi.fn(),
    onLogAmount: vi.fn().mockResolvedValue(undefined),
    onDeleteLoggedAmount: vi.fn(),
    history: [] as DoseLogEntry[],
    ...extra,
  };
  render(<DashboardTab {...props} />);
  return props;
}

describe('schedule: logging the amount actually taken', () => {
  it('opens a log form whose amount and unit start blank, even when the user saved a dose on the compound', async () => {
    renderDashboard([bpc]);

    await userEvent.click(screen.getByRole('button', { name: 'Log amount taken' }));

    expect((screen.getByLabelText('Amount taken') as HTMLInputElement).value).toBe('');
    expect((screen.getByLabelText('Unit taken') as HTMLSelectElement).value).toBe('');
    expect((screen.getByLabelText('Note (optional)') as HTMLInputElement).value).toBe('');
  });

  it('copies the user\'s own saved entry only when they press "Same as my saved entry"', async () => {
    renderDashboard([bpc]);
    await userEvent.click(screen.getByRole('button', { name: 'Log amount taken' }));

    // Present, but nothing has been copied yet.
    const same = screen.getByRole('button', { name: 'Same as my saved entry (250 mcg)' });
    expect((screen.getByLabelText('Amount taken') as HTMLInputElement).value).toBe('');

    await userEvent.click(same);

    expect((screen.getByLabelText('Amount taken') as HTMLInputElement).value).toBe('250');
    expect((screen.getByLabelText('Unit taken') as HTMLSelectElement).value).toBe('mcg');
  });

  it('has no "same as" button when the user saved no dose entry, and never fills anything in', async () => {
    renderDashboard([{ ...bpc, dose: null, doseAmount: null, doseUnit: null }]);

    await userEvent.click(screen.getByRole('button', { name: 'Log amount taken' }));

    expect(screen.queryByRole('button', { name: /Same as my saved entry/ })).not.toBeInTheDocument();
    expect((screen.getByLabelText('Amount taken') as HTMLInputElement).value).toBe('');
  });

  it('logs the amount the user typed, with an optional note, and closes the form', async () => {
    const { onLogAmount } = renderDashboard([bpc]);
    await userEvent.click(screen.getByRole('button', { name: 'Log amount taken' }));

    await userEvent.type(screen.getByLabelText('Amount taken'), '0.5');
    await userEvent.selectOptions(screen.getByLabelText('Unit taken'), 'mg');
    await userEvent.type(screen.getByLabelText('Note (optional)'), 'left side');
    await userEvent.click(screen.getByRole('button', { name: 'Save amount' }));

    await waitFor(() => expect(onLogAmount).toHaveBeenCalledTimes(1));
    expect(onLogAmount).toHaveBeenCalledWith(bpc, { amount: 0.5, unit: 'mg', note: 'left side' });
    await waitFor(() => expect(screen.queryByLabelText('Amount taken')).not.toBeInTheDocument());
  });

  it.each([
    ['', 'mcg', 'Enter the amount you took, as a number greater than zero.'],
    ['0', 'mcg', 'Enter the amount you took, as a number greater than zero.'],
    ['-5', 'mg', 'Enter the amount you took, as a number greater than zero.'],
    ['250', '', 'Choose the unit.'],
  ])('refuses amount "%s" / unit "%s" with a plain error and logs nothing', async (amount, unit, message) => {
    const { onLogAmount } = renderDashboard([bpc]);
    await userEvent.click(screen.getByRole('button', { name: 'Log amount taken' }));
    fireEvent.change(screen.getByLabelText('Amount taken'), { target: { value: amount } });
    fireEvent.change(screen.getByLabelText('Unit taken'), { target: { value: unit } });

    await userEvent.click(screen.getByRole('button', { name: 'Save amount' }));

    expect(await screen.findByText(message)).toBeInTheDocument();
    expect(onLogAmount).not.toHaveBeenCalled();
    expect(document.body.textContent?.toLowerCase()).not.toMatch(/recommended|typical dose|safe dose|too high|too low/);
  });

  it('keeps what the user typed and shows the error when logging fails', async () => {
    const onLogAmount = vi.fn().mockRejectedValue(new Error('Amount must be a number greater than zero.'));
    renderDashboard([bpc], { onLogAmount });
    await userEvent.click(screen.getByRole('button', { name: 'Log amount taken' }));
    await userEvent.type(screen.getByLabelText('Amount taken'), '250');
    await userEvent.selectOptions(screen.getByLabelText('Unit taken'), 'mcg');

    await userEvent.click(screen.getByRole('button', { name: 'Save amount' }));

    expect(await screen.findByText('Amount must be a number greater than zero.')).toBeInTheDocument();
    expect((screen.getByLabelText('Amount taken') as HTMLInputElement).value).toBe('250');
  });

  it('shows no log button for a row that has no compound id (an older response)', () => {
    renderDashboard([{ ...bpc, compoundId: null }]);

    expect(screen.queryByRole('button', { name: 'Log amount taken' })).not.toBeInTheDocument();
  });

  it('still offers the original "Log Today\'s Doses" button', async () => {
    const { onLogDoses } = renderDashboard([bpc]);

    await userEvent.click(screen.getByRole('button', { name: /Log Today.s Doses/ }));

    expect(onLogDoses).toHaveBeenCalledTimes(1);
  });
});

describe('schedule: frequency labels and logged amounts', () => {
  it('shows the user\'s chosen days and the amounts already logged', () => {
    renderDashboard([{ ...bpc, status: 'completed', logged: [logged({ note: 'left side' })] }]);

    expect(screen.getByText('Your schedule: Mon, Thu')).toBeInTheDocument();
    expect(screen.getByText('Logged: 250 mcg · left side')).toBeInTheDocument();
    expect(screen.getByText('Completed')).toBeInTheDocument();
  });

  it('shows N times per week as a weekly target with the count so far, not as a day', () => {
    renderDashboard([
      { ...bpc, cadence: '2 times per week (weekly target)', due: 'weekly_target', weekLogged: 1 },
    ]);

    expect(screen.getByText('Weekly target: 2 times per week · 1 logged this week')).toBeInTheDocument();
    expect(screen.queryByText(/Your schedule:/)).not.toBeInTheDocument();
  });

  it('labels as-needed entries as having no fixed day', () => {
    renderDashboard([{ ...bpc, cadence: 'As needed', due: 'as_needed' }]);

    expect(screen.getByText('As needed (no fixed day)')).toBeInTheDocument();
  });

  it('shows an older free-text-only entry exactly as before, with the user\'s own text', () => {
    renderDashboard([{ ...bpc, cadence: null, due: 'scheduled' }]);

    expect(screen.getByText('Your dose: 250 mcg · twice weekly')).toBeInTheDocument();
    expect(screen.queryByText(/Your schedule:/)).not.toBeInTheDocument();
  });

  it('says so when nothing is due on the day', () => {
    renderDashboard([]);

    expect(screen.getByText('Nothing on your schedule is due on this day.')).toBeInTheDocument();
  });

  it('shows the same labels and logged amounts in the day detail view', () => {
    render(<DayDetailModal day={day([{ ...bpc, logged: [logged()] }])} onClose={vi.fn()} />);

    expect(screen.getByText('Your schedule: Mon, Thu')).toBeInTheDocument();
    expect(screen.getByText('Logged: 250 mcg')).toBeInTheDocument();
  });
});

describe('history of logged amounts', () => {
  it('lists the amounts exactly as logged, newest first, with a delete button for each', async () => {
    const history = [
      logged({ id: 'e2', logDate: '2026-10-08', amount: 0.5, unit: 'mg', note: 'evening' }),
      logged({ id: 'e1', logDate: '2026-10-05' }),
    ];
    const { onDeleteLoggedAmount } = renderDashboard([bpc], { history });

    const list = screen.getByRole('list', { name: 'Logged amounts' });
    const rows = within(list).getAllByRole('listitem');
    expect(rows).toHaveLength(2);
    expect(rows[0]).toHaveTextContent('BPC-157');
    expect(rows[0]).toHaveTextContent('0.5 mg');
    expect(rows[0]).toHaveTextContent('evening');
    expect(rows[1]).toHaveTextContent('250 mcg');

    await userEvent.click(within(rows[1]).getByRole('button', { name: /Delete logged amount for BPC-157 on 2026-10-05/ }));
    expect(onDeleteLoggedAmount).toHaveBeenCalledWith(history[1]);
  });

  it('says so when nothing has been logged', () => {
    renderDashboard([bpc]);

    expect(screen.getByText('No amounts logged yet.')).toBeInTheDocument();
  });
});

describe('provider summary: logged amounts', () => {
  it('lists the user\'s logged amounts for this protocol\'s compounds, labelled as user-entered, on screen and in the copied text', async () => {
    const protocol = makeSavedProviderSummaryProtocol();
    const compoundId = protocol.items[0].compoundRecordId;
    const doseLog = [
      logged({ id: 'a', compoundId, compoundName: 'Fixture compound', amount: 250, unit: 'mcg', logDate: '2026-10-05', note: 'left side' }),
      logged({ id: 'b', compoundId: 'some-other-compound', compoundName: 'Elsewhere', amount: 9, unit: 'mg' }),
    ];
    const writeText = vi.fn().mockResolvedValue(undefined);
    Object.defineProperty(navigator, 'clipboard', { value: { writeText }, configurable: true });
    render(<ProviderObservationalSummary protocol={protocol} doseLog={doseLog} generatedAt={providerSummaryGeneratedAt} />);

    const list = screen.getByRole('list', { name: 'Logged amounts' });
    expect(within(list).getAllByRole('listitem')).toHaveLength(1);
    expect(list).toHaveTextContent('250 mcg on Oct 5, 2026');
    expect(list).toHaveTextContent('left side');
    expect(list).not.toHaveTextContent('Elsewhere');

    await userEvent.click(screen.getByRole('button', { name: 'Copy summary' }));
    expect(writeText).toHaveBeenCalledWith(expect.stringContaining('Logged amounts'));
    expect(writeText).toHaveBeenCalledWith(expect.stringContaining('250 mcg on Oct 5, 2026 (left side)'));
  });

  it('says "No logged amounts recorded" when the user logged none, and omits the section when no log was supplied', () => {
    const protocol = makeSavedProviderSummaryProtocol();
    const { unmount } = render(<ProviderObservationalSummary protocol={protocol} doseLog={[]} generatedAt={providerSummaryGeneratedAt} />);
    expect(screen.getByText('No logged amounts recorded')).toBeInTheDocument();
    unmount();

    render(<ProviderObservationalSummary protocol={protocol} generatedAt={providerSummaryGeneratedAt} />);
    expect(screen.queryByText('No logged amounts recorded')).not.toBeInTheDocument();
    expect(screen.queryByRole('list', { name: 'Logged amounts' })).not.toBeInTheDocument();
  });

  it('shows the user-chosen frequency next to their free text', () => {
    const protocol = makeSavedProviderSummaryProtocol();
    protocol.items[0].compound = {
      ...protocol.items[0].compound!,
      doseFrequency: 'twice weekly',
      frequencyKind: 'weekdays',
      frequencyWeekdays: [1, 4],
    };
    render(<ProviderObservationalSummary protocol={protocol} generatedAt={providerSummaryGeneratedAt} />);

    expect(screen.getByText('User-chosen frequency')).toBeInTheDocument();
    expect(screen.getByText('Mon, Thu')).toBeInTheDocument();
  });
});
