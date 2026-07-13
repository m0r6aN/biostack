import DecisionTheaterPage from '@/app/protocols/[id]/review/page';
import { apiClient } from '@/lib/api';
import { render, screen, waitFor } from '@testing-library/react';
import type { ReactNode } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('next/navigation', () => ({ useParams: () => ({ id: 'protocol-1' }) }));
vi.mock('next/link', () => ({
  default: ({ children, href }: { children: ReactNode; href: string }) => <a href={href}>{children}</a>,
}));
vi.mock('@/lib/flags', () => ({ isEnabled: () => true }));
vi.mock('@/lib/api', () => ({
  apiClient: {
    getProtocol: vi.fn(),
    postStackReviewEnvelope: vi.fn(),
  },
}));
vi.mock('@/components/intel/SafetyHierarchy', () => ({
  SafetyHierarchy: ({ deterministic, commentary }: { deterministic: ReactNode; commentary: ReactNode }) => (
    <div>{deterministic}{commentary}</div>
  ),
}));
vi.mock('@/components/governance/CognitiveHeatGauge', () => ({ CognitiveHeatGauge: () => null }));
vi.mock('@/components/governance/ConfidenceProfileCard', () => ({ ConfidenceProfileCard: () => null }));
vi.mock('@/components/governance/ClaimBadgeStack', () => ({ ClaimBadgeStack: () => null }));
vi.mock('@/components/governance/GovernedSentence', () => ({
  GovernedSentence: ({ text }: { text: string }) => <span>{text}</span>,
}));
vi.mock('@/components/governance/WitnessNarrativePanel', () => ({ WitnessNarrativePanel: () => null }));
vi.mock('@/components/governance/ReasoningGraphViewer', () => ({ ReasoningGraphViewer: () => null }));

describe('Decision Theater preview qualification', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(apiClient.getProtocol).mockResolvedValue({
      id: 'protocol-1',
      name: 'Observed protocol',
      items: [{ compound: { name: 'Magnesium', category: 'Mineral' } }],
      interactionIntelligence: { topFindings: [] },
      activeRun: null,
    } as never);
    vi.mocked(apiClient.postStackReviewEnvelope).mockResolvedValue({
      deterministicFindings: [],
      perspectiveReviews: {},
      contradictionReview: { counterPlanNarrative: '', isExecutable: false, effectStatus: 'commentary-only' },
      confidenceProfile: {
        model: 'moderate',
        epistemic: 'moderate',
        evidenceSupport: 'low',
        contradictionDensity: 'low',
        calibrationVersion: 'test',
      },
      reasoningGraph: { graphId: 'graph-1', nodeCount: 0, edgeCount: 0 },
      effectStatus: 'commentary-only',
      witnessNarrative: { entries: [] },
      reasoningGraphFull: { graphId: 'graph-1', nodes: [], edges: [] },
    } as never);
  });

  it('uses payload-only review and does not offer canonical protocol completion', async () => {
    render(<DecisionTheaterPage />);

    expect(await screen.findByText('Decision Theater Preview')).toBeInTheDocument();
    expect(screen.getByText(/not server-bound to the protocol record/i)).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Complete Review' })).not.toBeInTheDocument();

    await waitFor(() => expect(apiClient.postStackReviewEnvelope).toHaveBeenCalled());
    const request = vi.mocked(apiClient.postStackReviewEnvelope).mock.calls[0][0];
    expect(request).toHaveProperty('payload');
    expect(request).not.toHaveProperty('protocolId');
  });
});
