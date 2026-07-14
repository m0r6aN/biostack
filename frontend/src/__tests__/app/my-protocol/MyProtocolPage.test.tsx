import MyProtocolPage from '@/app/my-protocol/page';
import { apiClient } from '@/lib/api';
import type { ProtocolOverview } from '@/lib/types';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import type { ReactNode } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const pushMock = vi.fn();

vi.mock('next/navigation', () => ({
  useRouter: () => ({ push: pushMock }),
}));

vi.mock('next/link', () => ({
  default: ({ href, children, ...props }: { href: string; children: ReactNode }) => (
    <a href={href} {...props}>{children}</a>
  ),
}));

vi.mock('@/lib/context', () => ({
  useProfile: () => ({ currentProfileId: 'profile-1' }),
}));

vi.mock('@/lib/api', () => ({
  apiClient: {
    getCurrentSubscription: vi.fn(),
    getProtocolPortal: vi.fn(),
    getProtocolPortalActive: vi.fn(),
    getProtocolPortalSchedule: vi.fn(),
    getProtocolPortalSupplements: vi.fn(),
    getProtocolPortalResources: vi.fn(),
    getProtocolPortalWeek: vi.fn(),
    getProtocolPortalDiet: vi.fn(),
    getProtocolPortalMilestones: vi.fn(),
    getProtocolPortalMonitoring: vi.fn(),
    logProtocolDoses: vi.fn(),
    saveCareTeamNote: vi.fn(),
  },
}));

vi.mock('@/components/Header', () => ({ Header: ({ title }: { title: string }) => <h1>{title}</h1> }));
vi.mock('@/components/LoadingState', () => ({ LoadingSkeleton: () => <div>Loading portal</div> }));
vi.mock('@/components/EmptyState', () => ({ EmptyState: ({ title }: { title: string }) => <div>{title}</div> }));
vi.mock('@/components/ErrorState', () => ({ ErrorState: ({ message }: { message: string }) => <div>{message}</div> }));
vi.mock('@/components/protocol-portal/ProtocolOverviewHero', () => ({
  ProtocolOverviewHero: ({ overview }: { overview: ProtocolOverview }) => <div>{overview.protocolName}</div>,
}));
vi.mock('@/components/protocol-portal/ProtocolStatGrid', () => ({ ProtocolStatGrid: () => <div>Live stats</div> }));
vi.mock('@/components/protocol-portal/ProtocolTabBar', () => ({
  ProtocolTabBar: ({ onChange }: { onChange: (id: string) => void }) => (
    <div>
      <button onClick={() => onChange('calendar')}>Calendar &amp; Schedule</button>
      <button onClick={() => onChange('monitoring')}>Monitoring &amp; Labs</button>
    </div>
  ),
}));
vi.mock('@/components/protocol-portal/tabs/DashboardTab', () => ({
  DashboardTab: ({ onLogDoses, onSaveCareTeamNote }: { onLogDoses: () => void; onSaveCareTeamNote: () => void }) => (
    <div>
      <button onClick={onLogDoses}>Log live doses</button>
      <button onClick={onSaveCareTeamNote}>Open care note</button>
    </div>
  ),
}));
vi.mock('@/components/protocol-portal/tabs/CalendarTab', () => ({ CalendarTab: () => <div>Live calendar</div> }));
vi.mock('@/components/protocol-portal/tabs/DietLifestyleTab', () => ({ DietLifestyleTab: () => <div>Live diet</div> }));
vi.mock('@/components/protocol-portal/tabs/MonitoringLabsTab', () => ({ MonitoringLabsTab: () => <div>Live monitoring</div> }));
vi.mock('@/components/protocol-portal/tabs/ProgressMilestonesTab', () => ({ ProgressMilestonesTab: () => <div>Live milestones</div> }));
vi.mock('@/components/protocol-portal/tabs/ResourcesTab', () => ({ ResourcesTab: () => <div>Live resources</div> }));
vi.mock('@/components/protocol-portal/tabs/SupplementationTab', () => ({ SupplementationTab: () => <div>Live supplements</div> }));
vi.mock('@/components/protocol-portal/DayDetailModal', () => ({ DayDetailModal: () => <div>Day detail</div> }));
vi.mock('@/components/protocol-portal/ContactCareTeamModal', () => ({
  ContactCareTeamModal: ({ onSubmit }: { onSubmit: (message: string) => Promise<void> }) => (
    <button onClick={() => void onSubmit('Observed update')}>Save care note</button>
  ),
}));
vi.mock('@/components/protocol-portal/Toast', () => ({ Toast: ({ message }: { message: string }) => <div>{message}</div> }));

const overview: ProtocolOverview = {
  protocolName: 'Live Protocol',
  objective: 'Observe changes over time',
  status: 'active',
  startedOnUtc: '2026-07-01T00:00:00Z',
  clientName: 'Test Profile',
  clientAvatarUrl: null,
  currentPhase: { number: 1, label: 'Current', currentWeek: 1, totalWeeks: 4 },
  phases: [{ number: 1, label: 'Current', currentWeek: 1, totalWeeks: 4 }],
};

const today = {
  dateIso: '2026-07-10',
  title: 'Today',
  subtitle: 'Live schedule',
  items: [],
};

describe('/my-protocol live integration', () => {
  beforeEach(() => {
    vi.clearAllMocks();
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
    vi.mocked(apiClient.getProtocolPortalSchedule).mockResolvedValue(today);
    vi.mocked(apiClient.getProtocolPortalSupplements).mockResolvedValue({ title: 'Supplements', summary: '', entries: [], additional: [] });
    vi.mocked(apiClient.getProtocolPortalResources).mockResolvedValue([]);
    vi.mocked(apiClient.getProtocolPortalWeek).mockResolvedValue([]);
    vi.mocked(apiClient.getProtocolPortalDiet).mockResolvedValue({ title: 'Diet', summary: '', targets: [], rationale: '', lifestyle: [] });
    vi.mocked(apiClient.getProtocolPortalMilestones).mockResolvedValue([]);
    vi.mocked(apiClient.getProtocolPortalMonitoring).mockResolvedValue({ baselineCompleted: '', recurringCadence: '', recurringLabs: [], adjustmentRules: [] });
    vi.mocked(apiClient.logProtocolDoses).mockResolvedValue(undefined);
    vi.mocked(apiClient.saveCareTeamNote).mockResolvedValue(undefined);
  });

  it('loads live Observer endpoints and does not download the full paid aggregate', async () => {
    render(<MyProtocolPage />);

    expect(await screen.findByText('Live Protocol')).toBeInTheDocument();
    expect(apiClient.getProtocolPortalActive).toHaveBeenCalledWith('profile-1');
    expect(apiClient.getProtocolPortalSchedule).toHaveBeenCalledWith('profile-1');
    expect(apiClient.getProtocolPortalSupplements).toHaveBeenCalledWith('profile-1');
    expect(apiClient.getProtocolPortalResources).toHaveBeenCalledWith('profile-1');
    expect(apiClient.getProtocolPortal).not.toHaveBeenCalled();
    expect(apiClient.getProtocolPortalWeek).not.toHaveBeenCalled();
    expect(apiClient.getProtocolPortalMonitoring).not.toHaveBeenCalled();
  });

  it('renders an upgrade card for an Observer opening an Operator tab', async () => {
    const user = userEvent.setup();
    render(<MyProtocolPage />);
    await screen.findByText('Live Protocol');

    await user.click(screen.getByRole('button', { name: 'Calendar & Schedule' }));

    expect(screen.getByText('Operator — Track & Analyze')).toBeInTheDocument();
    expect(screen.queryByText('Live calendar')).not.toBeInTheDocument();
  });

  it('loads and renders Operator sections for an Operator subscription', async () => {
    vi.mocked(apiClient.getCurrentSubscription).mockResolvedValue({
      tier: 'Operator',
      status: 'Active',
      productCode: 'operator',
      isPaid: true,
      cancelAtPeriodEnd: false,
      currentPeriodEndUtc: '2026-08-10T00:00:00Z',
      features: {},
      limits: {},
    });
    const user = userEvent.setup();
    render(<MyProtocolPage />);
    await screen.findByText('Live Protocol');

    await waitFor(() => expect(apiClient.getProtocolPortalWeek).toHaveBeenCalledWith('profile-1'));
    await user.click(screen.getByRole('button', { name: 'Calendar & Schedule' }));

    expect(screen.getByText('Live calendar')).toBeInTheDocument();
    expect(apiClient.getProtocolPortalMonitoring).not.toHaveBeenCalled();
  });

  it('persists dose logs and care-team notes through live API actions', async () => {
    const user = userEvent.setup();
    render(<MyProtocolPage />);
    await screen.findByText('Live Protocol');

    await user.click(screen.getByRole('button', { name: 'Log live doses' }));
    await waitFor(() => expect(apiClient.logProtocolDoses).toHaveBeenCalledWith('profile-1', '2026-07-10'));

    await user.click(screen.getByRole('button', { name: 'Open care note' }));
    await user.click(screen.getByRole('button', { name: 'Save care note' }));
    await waitFor(() => expect(apiClient.saveCareTeamNote).toHaveBeenCalledWith('profile-1', 'Observed update'));
  });
});
