import ProtocolsPage from '@/app/protocols/page';
import { apiClient } from '@/lib/api';
import type { Protocol } from '@/lib/types';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import type { ReactNode } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { makeSavedProviderSummaryProtocol } from '../../fixtures/providerSummary';

vi.mock('next/link', () => ({
  default: ({ href, children, ...props }: { href: string; children: ReactNode }) => <a href={href} {...props}>{children}</a>,
}));

const { push } = vi.hoisted(() => ({ push: vi.fn() }));
vi.mock('next/navigation', () => ({ useRouter: () => ({ push }) }));

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
vi.mock('@/components/EmptyState', () => ({
  EmptyState: ({
    title,
    description,
    action,
    secondaryAction,
  }: {
    title: string;
    description: string;
    action?: { label: string; onClick: () => void };
    secondaryAction?: { label: string; href: string };
  }) => (
    <div>
      <h3>{title}</h3>
      <p>{description}</p>
      {action && <button onClick={action.onClick}>{action.label}</button>}
      {secondaryAction && <a href={secondaryAction.href}>{secondaryAction.label}</a>}
    </div>
  ),
}));
vi.mock('@/components/ErrorState', () => ({
  ErrorState: ({ title, message, onRetry }: { title?: string; message: string; onRetry?: () => void }) => (
    <div>
      <h3>{title}</h3>
      <p>{message}</p>
      {onRetry && <button onClick={onRetry}>Try Again</button>}
    </div>
  ),
}));
vi.mock('@/components/protocols/SimulationTimeline', () => ({ SimulationTimeline: () => <div>Simulation</div> }));
vi.mock('@/components/protocols/StackScoreCard', () => ({ StackScoreCard: () => <div>Stack score</div> }));

const profileState = vi.hoisted(() => ({ currentProfileId: 'person-1' as string | null }));
vi.mock('@/lib/context', () => ({
  useProfile: () => ({
    currentProfileId: profileState.currentProfileId,
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

describe('/protocols protocol list states', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    Element.prototype.scrollIntoView = vi.fn();
    profileState.currentProfileId = 'person-1';
    const protocol = makeSavedProviderSummaryProtocol();
    vi.mocked(apiClient.getProtocols).mockResolvedValue([protocol]);
    vi.mocked(apiClient.getCurrentStackIntelligence).mockResolvedValue({
      stackScore: protocol.stackScore,
      simulation: protocol.simulation,
      interactionIntelligence: protocol.interactionIntelligence,
    });
  });

  it('shows the loading state while protocols are being fetched', async () => {
    vi.mocked(apiClient.getProtocols).mockReturnValue(Promise.withResolvers<Protocol[]>().promise);
    render(<ProtocolsPage />);

    expect(screen.getByText('Loading protocols')).toBeInTheDocument();
    expect(screen.queryByText('No protocols saved yet')).not.toBeInTheDocument();
  });

  it('explains what protocols are and links to My Protocol when none are saved', async () => {
    vi.mocked(apiClient.getProtocols).mockResolvedValue([]);
    render(<ProtocolsPage />);

    expect(await screen.findByText('No protocols saved yet')).toBeInTheDocument();
    const explain = screen.getByText(/versioned snapshot of what this profile is running/i);
    expect(explain).toBeInTheDocument();
    const myProtocolLinks = screen.getAllByRole('link', { name: 'Open My Protocol' });
    expect(myProtocolLinks.length).toBeGreaterThan(0);
    for (const link of myProtocolLinks) {
      expect(link).toHaveAttribute('href', '/my-protocol');
    }
  });

  it('shows name, version state, and compound summary on each saved protocol card', async () => {
    render(<ProtocolsPage />);

    const card = (await screen.findByRole('link', { name: /Recovery Protocol/i })).closest('article');
    expect(card).not.toBeNull();
    expect(card?.textContent).toContain('v2');
    expect(card?.textContent).toContain('Active');
    expect(card?.textContent).toContain('Current version');
    expect(card?.textContent).toContain('1 item: Magnesium glycinate');
    expect(card?.textContent).toContain('No active run yet');
  });

  it('marks drafts and reports the active run on the protocol card', async () => {
    vi.mocked(apiClient.getProtocols).mockResolvedValue([
      makeSavedProviderSummaryProtocol({
        isDraft: true,
        isCurrentVersion: false,
        activeRun: {
          id: 'run-1',
          protocolId: 'protocol-2',
          personId: 'person-1',
          protocolName: 'Recovery Protocol',
          protocolVersion: 2,
          startedAtUtc: '2026-01-03T00:00:00Z',
          endedAtUtc: null,
          status: 'active',
          notes: '',
        },
      }),
    ]);
    render(<ProtocolsPage />);

    const card = (await screen.findByRole('link', { name: /Recovery Protocol/i })).closest('article');
    expect(card?.textContent).toContain('Draft');
    expect(card?.textContent).toContain('Prior version');
    expect(card?.textContent).toContain('Run active since');
  });

  it('explains the no-profile state instead of rendering a blank list', async () => {
    profileState.currentProfileId = null;
    render(<ProtocolsPage />);

    expect(await screen.findByText('Create a profile to start building protocols')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Create profile' })).toBeInTheDocument();
    expect(apiClient.getProtocols).not.toHaveBeenCalled();
  });

  it('explains a load failure and offers a retry', async () => {
    vi.mocked(apiClient.getProtocols).mockRejectedValue(new Error('failure'));
    render(<ProtocolsPage />);

    expect(await screen.findByText("Couldn't load protocols")).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Try Again' })).toBeInTheDocument();
  });
});

describe('/protocols saved protocol actions', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    Element.prototype.scrollIntoView = vi.fn();
    profileState.currentProfileId = 'person-1';
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
    expect(screen.getByRole('link', { name: 'Review protocol' })).toHaveAttribute('href', '/protocols/protocol-2');
  });

  it('does not add medical, advice, dosing, start, stop, or combine language to the saved card action', async () => {
    render(<ProtocolsPage />);

    const primaryLink = await screen.findByRole('link', { name: /Recovery Protocol/i });
    const cardText = primaryLink.closest('article')?.textContent ?? '';
    expect(cardText).toContain('Provider summary');
    expect(cardText).not.toMatch(/\b(medical|advice|dosing|dose|start|stop|combine|combined)\b/i);
  });

  it('hands tracking off to the named save form without creating or starting anything', async () => {
    render(<ProtocolsPage />);
    const handoff = await screen.findByRole('button', { name: 'Save this stack to start tracking' });
    fireEvent.click(handoff);
    expect(screen.getByRole('textbox', { name: 'Protocol name' })).toHaveFocus();
    expect(Element.prototype.scrollIntoView).toHaveBeenCalled();
    expect(apiClient.saveCurrentStackAsProtocol).not.toHaveBeenCalled();
    expect(push).not.toHaveBeenCalled();
  });

  it('requires a name and opens the explicitly saved protocol by its returned ID', async () => {
    vi.mocked(apiClient.saveCurrentStackAsProtocol).mockResolvedValue(makeSavedProviderSummaryProtocol({ id: 'new-saved-id' }));
    render(<ProtocolsPage />);
    await screen.findByRole('button', { name: 'Save this stack to start tracking' });
    const save = screen.getByRole('button', { name: 'Save' });
    expect(save).toBeDisabled();
    fireEvent.change(screen.getByRole('textbox', { name: 'Protocol name' }), { target: { value: '   ' } });
    expect(save).toBeDisabled();
    fireEvent.click(save);
    expect(apiClient.saveCurrentStackAsProtocol).not.toHaveBeenCalled();
    fireEvent.change(screen.getByRole('textbox', { name: 'Protocol name' }), { target: { value: 'My tracked stack' } });
    fireEvent.click(save);
    await waitFor(() => expect(push).toHaveBeenCalledWith('/protocols/new-saved-id'));
    expect(apiClient.saveCurrentStackAsProtocol).toHaveBeenCalledExactlyOnceWith('person-1', 'My tracked stack');
  });

  it('keeps the name and save form available after a failed explicit save', async () => {
    vi.mocked(apiClient.saveCurrentStackAsProtocol).mockRejectedValue(new Error('failure'));
    render(<ProtocolsPage />);
    await screen.findByRole('button', { name: 'Save this stack to start tracking' });
    const name = screen.getByRole('textbox', { name: 'Protocol name' });
    fireEvent.change(name, { target: { value: 'Keep my name' } });
    fireEvent.click(screen.getByRole('button', { name: 'Save' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('Save failed');
    expect(name).toHaveValue('Keep my name');
    expect(screen.getByRole('button', { name: 'Save' })).toBeEnabled();
    expect(push).not.toHaveBeenCalled();
  });

  it('prevents a second explicit save while the first is pending', async () => {
    let finish!: (value: Protocol) => void;
    vi.mocked(apiClient.saveCurrentStackAsProtocol).mockImplementation(() => {
      const { promise, resolve } = Promise.withResolvers<Protocol>();
      finish = resolve;
      return promise;
    });
    render(<ProtocolsPage />);
    await screen.findByRole('button', { name: 'Save this stack to start tracking' });
    fireEvent.change(screen.getByRole('textbox', { name: 'Protocol name' }), { target: { value: 'One save' } });
    fireEvent.click(screen.getByRole('button', { name: 'Save' }));
    const pending = screen.getByRole('button', { name: 'Saving' });
    expect(pending).toBeDisabled();
    fireEvent.click(pending);
    expect(apiClient.saveCurrentStackAsProtocol).toHaveBeenCalledTimes(1);
    finish(makeSavedProviderSummaryProtocol());
    await waitFor(() => expect(push).toHaveBeenCalledWith('/protocols/protocol-2'));
  });

});
