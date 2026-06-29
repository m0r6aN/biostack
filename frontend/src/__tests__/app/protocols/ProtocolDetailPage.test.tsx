import ProtocolDetailPage from '@/app/protocols/[id]/page';
import { apiClient } from '@/lib/api';
import { act, render, screen, waitFor, type RenderResult } from '@testing-library/react';
import type { ReactNode } from 'react';
import { Suspense } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { makeSavedProviderSummaryProtocol } from '../../fixtures/providerSummary';

vi.mock('next/link', () => ({
  default: ({ href, children, ...props }: { href: string; children: ReactNode }) => <a href={href} {...props}>{children}</a>,
}));

vi.mock('next/navigation', () => ({
  useRouter: () => ({ push: vi.fn() }),
}));

vi.mock('@/components/Header', () => ({
  Header: ({ title, actions }: { title: string; actions?: ReactNode }) => (
    <header>
      <h1>{title}</h1>
      {actions}
    </header>
  ),
}));

vi.mock('@/components/LoadingState', () => ({ LoadingSkeleton: () => <div>Loading protocol</div> }));
vi.mock('@/components/EmptyState', () => ({ EmptyState: ({ title }: { title: string }) => <div>{title}</div> }));
vi.mock('@/components/ErrorState', () => ({ ErrorState: ({ message }: { message: string }) => <div>{message}</div> }));
vi.mock('@/components/protocols/ProtocolContinuityStrip', () => ({ ProtocolContinuityStrip: () => <div>Continuity</div> }));
vi.mock('@/components/dashboard/PatternMemoryPanel', () => ({ PatternMemoryPanel: () => <div>Patterns</div> }));
vi.mock('@/components/dashboard/DriftRegimePanel', () => ({ DriftRegimePanel: () => <div>Drift</div> }));
vi.mock('@/components/dashboard/SequenceExpectationPanel', () => ({ SequenceExpectationPanel: () => <div>Sequence</div> }));
vi.mock('@/components/protocols/SimulationTimeline', () => ({ SimulationTimeline: () => <div>Simulation</div> }));
vi.mock('@/components/protocols/InteractionIntelligenceCard', () => ({ InteractionIntelligenceCard: () => <div>Interactions</div> }));
vi.mock('@/components/protocols/StackScoreCard', () => ({ StackScoreCard: () => <div>Stack score</div> }));
vi.mock('@/components/protocols/ProtocolComparison', () => ({ ProtocolComparison: () => <div>Comparison</div> }));
vi.mock('@/components/protocols/ProtocolIntelligenceReview', () => ({ ProtocolIntelligenceReview: () => <div>Review</div> }));
vi.mock('@/components/protocols/ProviderObservationalSummary', () => ({ ProviderObservationalSummary: () => <div>Provider summary body</div> }));

vi.mock('@/lib/api', () => ({
  ApiError: class ApiError extends Error { upgradeRequired = false; },
  apiClient: {
    getProtocol: vi.fn(),
    getProtocolIntelligence: vi.fn(),
    getProtocolReview: vi.fn(),
    getProtocolPatterns: vi.fn(),
    getProtocolDrift: vi.fn(),
    getProtocolSequenceExpectation: vi.fn(),
    startProtocolRun: vi.fn(),
    completeProtocolRun: vi.fn(),
    abandonProtocolRun: vi.fn(),
    evolveProtocolFromRun: vi.fn(),
    completeProtocolReview: vi.fn(),
  },
}));

async function renderPage() {
  let result: RenderResult | undefined;
  const params = Promise.resolve({ id: 'protocol-2' });

  await act(async () => {
    result = render(
      <Suspense fallback={<div>Loading route</div>}>
        <ProtocolDetailPage params={params} />
      </Suspense>
    );
  });

  return result!;
}

describe('/protocols/[id] provider summary access', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(apiClient.getProtocol).mockResolvedValue(makeSavedProviderSummaryProtocol());
    vi.mocked(apiClient.getProtocolIntelligence).mockResolvedValue({
      status: 'Unknown',
      phaseMap: [],
      relationships: [],
      ambiguitySignals: [],
      sourceQualityWarnings: [],
      highRiskWarnings: [],
      unknowns: ['No reviewed Protocol Intelligence artifact exists for this protocol context.'],
      safetyNotes: [],
      upgradeHooks: [],
    });
    vi.mocked(apiClient.getProtocolReview).mockResolvedValue(null);
    vi.mocked(apiClient.getProtocolIntelligence).mockResolvedValue({
      status: 'Unknown',
      phaseMap: [],
      relationships: [],
      ambiguitySignals: [],
      sourceQualityWarnings: [],
      highRiskWarnings: [],
      unknowns: ['No reviewed Protocol Intelligence artifact exists for this protocol context.'],
      safetyNotes: [],
      upgradeHooks: [],
    });
    vi.mocked(apiClient.getProtocolPatterns).mockResolvedValue(null);
    vi.mocked(apiClient.getProtocolDrift).mockResolvedValue(null);
    vi.mocked(apiClient.getProtocolSequenceExpectation).mockResolvedValue(null);
  });

  it('renders a provider summary CTA in the saved protocol detail header', async () => {
    await renderPage();

    expect(await screen.findByRole('link', { name: 'Prepare provider summary' })).toBeInTheDocument();
  });

  it('targets the existing provider summary anchor', async () => {
    await renderPage();

    expect(await screen.findByRole('link', { name: 'Prepare provider summary' })).toHaveAttribute('href', '#provider-summary');
  });

  it('renders the provider summary section with the expected anchor id', async () => {
    const { container } = await renderPage();

    await waitFor(() => expect(container.querySelector('#provider-summary')).toBeInTheDocument());
  });

  it('smoke-renders the provider summary CTA and anchored section together', async () => {
    const { container } = await renderPage();

    expect(await screen.findByRole('link', { name: 'Prepare provider summary' })).toHaveAttribute('href', '#provider-summary');
    await waitFor(() => {
      const providerSummarySection = container.querySelector('#provider-summary');
      expect(providerSummarySection).toBeInTheDocument();
      expect(providerSummarySection).toHaveTextContent('Provider summary body');
    });
  });
});

describe('/protocols/[id] branch-from-run button', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(apiClient.getProtocolReview).mockResolvedValue(null);
    vi.mocked(apiClient.getProtocolPatterns).mockResolvedValue(null);
    vi.mocked(apiClient.getProtocolDrift).mockResolvedValue(null);
    vi.mocked(apiClient.getProtocolSequenceExpectation).mockResolvedValue(null);
  });

  it('renders "Branch from run" and not "Evolve from run" when a completed run exists', async () => {
    vi.mocked(apiClient.getProtocol).mockResolvedValue(
      makeSavedProviderSummaryProtocol({
        actualComparison: {
          simulation: { timeline: [], insights: [] },
          run: {
            id: 'run-1', protocolId: 'protocol-2', personId: 'person-1',
            protocolName: 'Recovery Protocol', protocolVersion: 2,
            startedAtUtc: '2026-01-03T00:00:00Z', endedAtUtc: '2026-02-01T00:00:00Z',
            status: 'completed', notes: '',
          },
          runSummary: null, observations: [], actualTrends: [], insights: [], highlights: [],
        },
      }),
    );

    await renderPage();

    await waitFor(() => expect(screen.getByRole('button', { name: 'Branch from run' })).toBeInTheDocument());
    expect(screen.queryByRole('button', { name: 'Evolve from run' })).not.toBeInTheDocument();
  });
});
