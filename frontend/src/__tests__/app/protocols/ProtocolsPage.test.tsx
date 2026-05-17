import ProtocolsPage from '@/app/protocols/page';
import { apiClient } from '@/lib/api';
import { render, screen } from '@testing-library/react';
import type { ReactNode } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { makeSavedProviderSummaryProtocol } from '../../fixtures/providerSummary';

vi.mock('next/link', () => ({
  default: ({ href, children, ...props }: { href: string; children: ReactNode }) => <a href={href} {...props}>{children}</a>,
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
vi.mock('@/components/LoadingState', () => ({ LoadingSkeleton: () => <div>Loading protocols</div> }));
vi.mock('@/components/EmptyState', () => ({ EmptyState: ({ title }: { title: string }) => <div>{title}</div> }));
vi.mock('@/components/ErrorState', () => ({ ErrorState: ({ message }: { message: string }) => <div>{message}</div> }));
vi.mock('@/components/protocols/SimulationTimeline', () => ({ SimulationTimeline: () => <div>Simulation</div> }));
vi.mock('@/components/protocols/InteractionIntelligenceCard', () => ({ InteractionIntelligenceCard: () => <div>Interactions</div> }));
vi.mock('@/components/protocols/StackScoreCard', () => ({ StackScoreCard: () => <div>Stack score</div> }));

vi.mock('@/lib/context', () => ({
  useProfile: () => ({
    currentProfileId: 'person-1',
    setCurrentProfileId: vi.fn(),
    profiles: [],
    setProfiles: vi.fn(),
    isSidebarOpen: false,
    setSidebarOpen: vi.fn(),
  }),
}));

vi.mock('@/lib/api', () => ({
  ApiError: class ApiError extends Error { upgradeRequired = false; },
  apiClient: {
    getProtocols: vi.fn(),
    getCurrentStackIntelligence: vi.fn(),
    saveCurrentStackAsProtocol: vi.fn(),
  },
}));

describe('/protocols saved protocol list', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    const protocol = makeSavedProviderSummaryProtocol();
    vi.mocked(apiClient.getProtocols).mockResolvedValue([protocol]);
    vi.mocked(apiClient.getCurrentStackIntelligence).mockResolvedValue({
      stackScore: protocol.stackScore,
      simulation: protocol.simulation,
      interactionIntelligence: protocol.interactionIntelligence,
    });
  });

  it('renders a provider summary link on saved protocol cards', async () => {
    render(<ProtocolsPage />);

    expect(await screen.findByRole('link', { name: 'Provider summary' })).toBeInTheDocument();
  });

  it('targets the saved protocol provider summary anchor', async () => {
    render(<ProtocolsPage />);

    expect(await screen.findByRole('link', { name: 'Provider summary' })).toHaveAttribute('href', '/protocols/protocol-2#provider-summary');
  });

  it('keeps the primary saved protocol action present', async () => {
    render(<ProtocolsPage />);

    expect(await screen.findByRole('link', { name: /Recovery Protocol/i })).toHaveAttribute('href', '/protocols/protocol-2');
  });

  it('does not add medical, advice, dosing, start, stop, or combine language to the saved card action', async () => {
    render(<ProtocolsPage />);

    const primaryLink = await screen.findByRole('link', { name: /Recovery Protocol/i });
    const cardText = primaryLink.closest('article')?.textContent ?? '';
    expect(cardText).toContain('Provider summary');
    expect(cardText).not.toMatch(/\b(medical|advice|dosing|dose|start|stop|combine|combined)\b/i);
  });
});