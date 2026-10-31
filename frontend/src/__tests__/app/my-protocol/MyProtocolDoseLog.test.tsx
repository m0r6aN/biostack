import MyProtocolPage from '@/app/my-protocol/page';
import { ApiClient, apiClient } from '@/lib/api';
import type { DaySchedule, DoseLogEntry, ProtocolOverview, ScheduleItem } from '@/lib/types';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import type { ReactNode } from 'react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('next/navigation', () => ({ useRouter: () => ({ push: vi.fn() }) }));
vi.mock('next/link', () => ({
  default: ({ href, children, ...props }: { href: string; children: ReactNode }) => <a href={href} {...props}>{children}</a>,
}));
vi.mock('@/lib/context', () => ({ useProfile: () => ({ currentProfileId: 'profile-1' }) }));
vi.mock('@/lib/api', async (importOriginal) => ({
  ...(await importOriginal<typeof import('@/lib/api')>()),
  apiClient: {
    getCurrentSubscription: vi.fn(),
    getProtocolPortalActive: vi.fn(),
    getProtocolPortalSchedule: vi.fn(),
    getProtocolPortalResources: vi.fn(),
    getProtocolPortalWeek: vi.fn(),
    getProtocolPortalDiet: vi.fn(),
    getProtocolPortalMilestones: vi.fn(),
    getProtocolPortalMonitoring: vi.fn(),
    getProtocolDoseHistory: vi.fn(),
    deleteProtocolDoseEntry: vi.fn(),
    logProtocolDoses: vi.fn(),
    saveCareTeamNote: vi.fn(),
  },
}));
vi.mock('@/components/Header', () => ({ Header: ({ title }: { title: string }) => <h1>{title}</h1> }));
vi.mock('@/components/protocol-portal/ProtocolOverviewHero', () => ({
  ProtocolOverviewHero: ({ overview }: { overview: ProtocolOverview }) => <div>{overview.protocolName}</div>,
}));
vi.mock('@/components/protocol-portal/ProtocolStatGrid', () => ({ ProtocolStatGrid: () => <div>Live stats</div> }));

const overview: ProtocolOverview = {
  protocolName: 'Live Protocol',
  objective: 'Observe changes over time',
  status: 'active',
  startedOnUtc: '2026-10-01T00:00:00Z',
  clientName: 'Test Profile',
  clientAvatarUrl: null,
  currentPhase: { number: 1, label: 'Current', currentWeek: 1, totalWeeks: 4 },
  phases: [{ number: 1, label: 'Current', currentWeek: 1, totalWeeks: 4 }],
};

// What the server returns for "250 mcg, twice weekly, Mon and Thu": due on Monday, not on Tuesday.
const bpcOnMonday: ScheduleItem = {
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
const monday: DaySchedule = { dateIso: '2026-10-05', title: 'Today\u2019s Schedule', subtitle: 'Monday', items: [bpcOnMonday] };
const tuesday: DaySchedule = { dateIso: '2026-10-06', title: 'Tuesday, October 6', subtitle: 'Tuesday', items: [] };

const entry: DoseLogEntry = {
  id: 'entry-1',
  compoundId: 'compound-1',
  compoundName: 'BPC-157',
  logDate: '2026-10-05',
  takenAtUtc: '2026-10-05T14:00:00Z',
  amount: 250,
  unit: 'mcg',
  note: null,
};

beforeEach(() => {
  vi.resetAllMocks();
  vi.mocked(apiClient.getCurrentSubscription).mockResolvedValue({
    tier: 'Observer',
    status: 'None',
    productCode: 'observer',
    isPaid: false,
    cancelAtPeriodEnd: false,
    currentPeriodEndUtc: null,
    features: {},
    limits: {},
  });
  vi.mocked(apiClient.getProtocolPortalActive).mockResolvedValue({ overview, stats: [] });
  vi.mocked(apiClient.getProtocolPortalSchedule).mockResolvedValue(monday);
  vi.mocked(apiClient.getProtocolPortalResources).mockResolvedValue([]);
  vi.mocked(apiClient.getProtocolDoseHistory).mockResolvedValue([]);
  vi.mocked(apiClient.logProtocolDoses).mockResolvedValue(undefined);
});

describe('/my-protocol: dose log pipeline (250 mcg twice weekly on Mon/Thu)', () => {
  it('shows the compound on its due day, logs the typed 250 mcg on Monday, and shows it in the history', async () => {
    const user = userEvent.setup();
    render(<MyProtocolPage />);

    // Monday: due.
    expect(await screen.findByText('BPC-157')).toBeInTheDocument();
    expect(screen.getByText('Your dose: 250 mcg · twice weekly')).toBeInTheDocument();
    expect(screen.getByText('Your schedule: Mon, Thu')).toBeInTheDocument();
    expect(screen.getByText('No amounts logged yet.')).toBeInTheDocument();

    // The amount starts blank; the user types it themselves.
    await user.click(screen.getByRole('button', { name: /^Log amount taken/ }));
    expect((screen.getByLabelText('Amount taken') as HTMLInputElement).value).toBe('');
    expect((screen.getByLabelText('Unit taken') as HTMLSelectElement).value).toBe('');

    // After the log, the server returns the day marked completed with the entry, and the history has it.
    vi.mocked(apiClient.getProtocolPortalSchedule).mockResolvedValue({
      ...monday,
      items: [{ ...bpcOnMonday, status: 'completed', logged: [entry] }],
    });
    vi.mocked(apiClient.getProtocolDoseHistory).mockResolvedValue([entry]);

    await user.type(screen.getByLabelText('Amount taken'), '250');
    await user.selectOptions(screen.getByLabelText('Unit taken'), 'mcg');
    await user.click(screen.getByRole('button', { name: 'Save amount' }));

    await waitFor(() => expect(apiClient.logProtocolDoses).toHaveBeenCalledTimes(1));
    expect(apiClient.logProtocolDoses).toHaveBeenCalledWith('profile-1', '2026-10-05', [
      { compoundId: 'compound-1', amount: 250, unit: 'mcg', note: null },
    ]);

    expect(await screen.findByText('Logged: 250 mcg')).toBeInTheDocument();
    expect(screen.getByText('Completed')).toBeInTheDocument();
    const history = screen.getByRole('list', { name: 'Logged amounts' });
    expect(within(history).getByText('BPC-157')).toBeInTheDocument();
    expect(within(history).getByText('250 mcg')).toBeInTheDocument();
  });

  it('does not report a successful log as failed when the subsequent refresh fails', async () => {
    const user = userEvent.setup();
    render(<MyProtocolPage />);
    await screen.findByText('BPC-157');
    await user.click(screen.getByRole('button', { name: /^Log amount taken/ }));
    await user.type(screen.getByLabelText('Amount taken'), '250');
    await user.selectOptions(screen.getByLabelText('Unit taken'), 'mcg');
    vi.mocked(apiClient.getProtocolPortalSchedule).mockRejectedValueOnce(new Error('refresh failed'));
    await user.click(screen.getByRole('button', { name: 'Save amount' }));
    expect(await screen.findByText('Amount logged, but the view could not be refreshed. Reload to see it.')).toBeInTheDocument();
    expect(apiClient.logProtocolDoses).toHaveBeenCalledTimes(1);
    expect(screen.queryByRole('button', { name: 'Save amount' })).not.toBeInTheDocument();
  });

  it('shows nothing due on a day the compound is not scheduled for', async () => {
    vi.mocked(apiClient.getProtocolPortalSchedule).mockResolvedValue(tuesday);
    render(<MyProtocolPage />);

    expect(await screen.findByText('Nothing on your schedule is due on this day.')).toBeInTheDocument();
    expect(screen.queryByText('BPC-157')).not.toBeInTheDocument();
  });

  it('deletes a logged amount and refreshes the day and the history', async () => {
    const user = userEvent.setup();
    vi.mocked(apiClient.getProtocolDoseHistory).mockResolvedValue([entry]);
    vi.mocked(apiClient.deleteProtocolDoseEntry).mockResolvedValue(undefined);
    render(<MyProtocolPage />);
    await screen.findByRole('list', { name: 'Logged amounts' });

    vi.mocked(apiClient.getProtocolDoseHistory).mockResolvedValue([]);
    await user.click(screen.getByRole('button', { name: /Delete logged amount for BPC-157/ }));

    await waitFor(() => expect(apiClient.deleteProtocolDoseEntry).toHaveBeenCalledWith('profile-1', 'entry-1'));
    expect(await screen.findByText('No amounts logged yet.')).toBeInTheDocument();
  });

  it('keeps the portal working when the history cannot be loaded', async () => {
    vi.mocked(apiClient.getProtocolDoseHistory).mockRejectedValue(new Error('history down'));
    render(<MyProtocolPage />);

    expect(await screen.findByText('BPC-157')).toBeInTheDocument();
    expect(screen.getByText('No amounts logged yet.')).toBeInTheDocument();
  });

  it('keeps the old "Log Today\'s Doses" behaviour: a date-only request with no entries', async () => {
    const user = userEvent.setup();
    render(<MyProtocolPage />);
    await screen.findByText('BPC-157');

    await user.click(screen.getByRole('button', { name: /Log Today.s Doses/ }));

    await waitFor(() => expect(apiClient.logProtocolDoses).toHaveBeenCalledWith('profile-1', '2026-10-05'));
  });
});

describe('ApiClient: portal reads and dose log requests', () => {
  const okResponse = (body: unknown, status = 200) => ({ ok: true, status, json: async () => body });

  afterEach(() => vi.unstubAllGlobals());

  function clientWithFetch(response: unknown = okResponse({})) {
    const fetchMock = vi.fn().mockResolvedValue(response);
    vi.stubGlobal('fetch', fetchMock);
    return { client: new ApiClient('https://api.example.test'), fetchMock };
  }

  it('sends the browser time zone so the server picks the user\'s "today"', async () => {
    vi.spyOn(Intl, 'DateTimeFormat').mockImplementation(
      () => ({ resolvedOptions: () => ({ timeZone: 'America/New_York' }) }) as unknown as Intl.DateTimeFormat,
    );
    const { client, fetchMock } = clientWithFetch();

    await client.getProtocolPortalSchedule('p1');
    await client.getProtocolPortalSchedule('p1', '2026-10-05');
    await client.getProtocolPortalWeek('p1', '2026-10-04');
    await client.getProtocolPortalActive('p1');
    await client.getProtocolPortal('p1');
    vi.restoreAllMocks();

    const urls = fetchMock.mock.calls.map((call) => String(call[0]));
    expect(urls[0]).toBe('https://api.example.test/api/v1/profiles/p1/protocol/schedule?tz=America%2FNew_York');
    expect(urls[1]).toBe('https://api.example.test/api/v1/profiles/p1/protocol/schedule?date=2026-10-05&tz=America%2FNew_York');
    expect(urls[2]).toBe('https://api.example.test/api/v1/profiles/p1/protocol/schedule/week?start=2026-10-04&tz=America%2FNew_York');
    expect(urls[3]).toBe('https://api.example.test/api/v1/profiles/p1/protocol/active?tz=America%2FNew_York');
    expect(urls[4]).toBe('https://api.example.test/api/v1/profiles/p1/protocol/portal?tz=America%2FNew_York');
  });

  it('sends a date-only body for "mark the day taken", exactly as before', async () => {
    const { client, fetchMock } = clientWithFetch(okResponse(undefined, 204));

    await client.logProtocolDoses('p1', '2026-10-05');

    const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(url).toBe('https://api.example.test/api/v1/profiles/p1/protocol/doses/log');
    expect(init.method).toBe('POST');
    expect(JSON.parse(String(init.body))).toEqual({ date: '2026-10-05' });
  });

  it('sends the typed amounts per compound when entries are given', async () => {
    const { client, fetchMock } = clientWithFetch(okResponse(undefined, 204));

    await client.logProtocolDoses('p1', '2026-10-05', [{ compoundId: 'c1', amount: 250, unit: 'mcg', note: null }]);

    const [, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(JSON.parse(String(init.body))).toEqual({
      date: '2026-10-05',
      entries: [{ compoundId: 'c1', amount: 250, unit: 'mcg', note: null }],
    });
  });

  it('reads the history with its filters and deletes one entry', async () => {
    const { client, fetchMock } = clientWithFetch(okResponse([]));

    await client.getProtocolDoseHistory('p1', { from: '2026-10-01', to: '2026-10-31', limit: 20 });
    await client.getProtocolDoseHistory('p1');
    await client.deleteProtocolDoseEntry('p1', 'e1');

    const calls = fetchMock.mock.calls as [string, RequestInit | undefined][];
    expect(calls[0][0]).toBe('https://api.example.test/api/v1/profiles/p1/protocol/doses/history?from=2026-10-01&to=2026-10-31&limit=20');
    expect(calls[1][0]).toBe('https://api.example.test/api/v1/profiles/p1/protocol/doses/history');
    expect(calls[2][0]).toBe('https://api.example.test/api/v1/profiles/p1/protocol/doses/log/e1');
    expect(calls[2][1]?.method).toBe('DELETE');
  });
});
