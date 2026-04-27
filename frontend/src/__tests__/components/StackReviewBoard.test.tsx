/**
 * Stack Review Board — UI tests (assertions 9 & 10 from the spec).
 *
 * T9: Safety panel renders ABOVE the Stack Review Board divider.
 * T10: Non-executable badge is present.
 *
 * Additional assertions:
 * T9a: All 5 cognitive panels are rendered.
 * T9b: BioStack KnownPatterns are rendered and distinguished from
 *       Historian findings.
 * T10a: Challenge panel contains a non-executable counter-plan label.
 * T10b: ConfidenceProfile sidebar is rendered when review is present.
 * T10c: Loading skeleton is shown when loading=true.
 */

import { render, screen, within } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { StackReviewBoard } from '@/components/tools/StackReviewBoard';
import type {
  SrbCognitiveDensityEnvelope,
  SrbDeterministicFinding,
  SrbKnownPattern,
} from '@/lib/types';

// ── Fixtures ──────────────────────────────────────────────────────────────────

const deterministicFindings: SrbDeterministicFinding[] = [
  {
    findingId: 'f001',
    code: 'SAF-001',
    category: 'ProviderReview',
    narrative: 'Injectable peptides require provider review.',
    compoundSlugs: ['bpc-157', 'tb-500'],
    riskScoreContribution: 0.15,
    evidenceTier: 'Anecdotal',
  },
];

const knownPatterns: SrbKnownPattern[] = [
  {
    patternId: 'bpc157-tb500-recovery',
    name: 'BPC-157 + TB-500 Regenerative Pairing',
    matchedCompoundSlugs: ['bpc-157', 'tb-500'],
    description: 'Classic regenerative pairing for tissue recovery protocols.',
  },
];

const mockReview: SrbCognitiveDensityEnvelope = {
  branchPerspectiveReview: {
    Optimizer: {
      kind: 'Optimizer',
      findings: [
        {
          findingId: 'OPT-001',
          category: 'GoalAlignment',
          narrative: 'Stack intent carries 2 evidence-tagged claims.',
          severity: 'Info',
        },
      ],
      summary: 'Goal-alignment and sequencing review complete.',
    },
    Skeptic: {
      kind: 'Skeptic',
      findings: [
        {
          findingId: 'SKP-001',
          category: 'AttributionRisk',
          narrative: 'Anecdotal evidence tier on bpc-157.',
          severity: 'Warning',
        },
      ],
      summary: 'Evidence gap and attribution risk review complete.',
    },
    Regulator: {
      kind: 'Regulator',
      findings: [
        {
          findingId: 'REG-001',
          category: 'ProviderReviewPressure',
          narrative: 'Risk score exceeds threshold. Provider review warranted.',
          severity: 'Warning',
        },
      ],
      summary: 'Claim risk and provider-review framing complete.',
    },
    Historian: {
      kind: 'Historian',
      findings: [
        {
          findingId: 'HST-001',
          category: 'PatternMemory',
          narrative: 'Pattern memory from BioStack: BPC-157 + TB-500 Regenerative Pairing.',
          severity: 'Info',
        },
      ],
      summary: 'Pattern recognition and collapse history review complete.',
    },
  },
  contradictionReview: {
    counterPlanNarrative: 'No executable counter-position generated. This is commentary only.',
    counterPlanIsExecutable: false,
    isExecutable: false,
  },
  confidenceProfile: {
    model: 'keon.collective-stub-v1',
    epistemic: 'partial',
    evidenceSupport: 'sparse',
    contradictionDensity: 'low',
    calibrationVersion: '1.0.0',
  },
  reasoningGraphRef: {
    graphId: 'rg::test-intent',
    nodeCount: 2,
    edgeCount: 1,
  },
};

// ── Tests ──────────────────────────────────────────────────────────────────────

describe('StackReviewBoard', () => {
  function renderBoard(props: Partial<Parameters<typeof StackReviewBoard>[0]> = {}) {
    return render(
      <StackReviewBoard
        deterministicFindings={deterministicFindings}
        knownPatterns={knownPatterns}
        review={mockReview}
        {...props}
      />,
    );
  }

  // T9: Safety panel renders ABOVE the divider (DOM order)
  it('T9 — deterministic safety panel precedes the SRB divider in DOM order', () => {
    renderBoard();
    const board = screen.getByTestId('stack-review-board');
    const safetyPanel = within(board).getByTestId('deterministic-safety-panel');
    const divider = within(board).getByTestId('srb-divider');

    // compareDocumentPosition: 4 = DOCUMENT_POSITION_FOLLOWING (divider comes after)
    expect(safetyPanel.compareDocumentPosition(divider) & Node.DOCUMENT_POSITION_FOLLOWING).toBe(
      Node.DOCUMENT_POSITION_FOLLOWING,
    );
  });

  // T10: Non-executable badge present
  it('T10 — non-executable badge is rendered', () => {
    renderBoard();
    expect(screen.getByTestId('non-executable-badge')).toBeInTheDocument();
    expect(screen.getByTestId('non-executable-badge')).toHaveTextContent(
      /non-executable/i,
    );
  });

  // T9a: All 5 cognitive panels rendered
  it('T9a — renders all five cognitive panels', () => {
    renderBoard();
    expect(screen.getByText('Goal Alignment & Sequencing')).toBeInTheDocument();
    expect(screen.getByText('Evidence Gaps & Attribution Risk')).toBeInTheDocument();
    expect(screen.getByText('Claim Risk & Provider Review')).toBeInTheDocument();
    expect(screen.getByText('Pattern Recognition')).toBeInTheDocument();
    expect(screen.getByText('Challenge This Stack')).toBeInTheDocument();
  });

  // T9b: BioStack KnownPatterns rendered and distinguished
  it('T9b — BioStack KnownPatterns render with "BioStack Known-Pattern Memory" label', () => {
    renderBoard();
    expect(
      screen.getByText(/BioStack Known-Pattern Memory/i),
    ).toBeInTheDocument();
    expect(
      screen.getByTestId('known-pattern-bpc157-tb500-recovery'),
    ).toBeInTheDocument();
    expect(screen.getByText('BPC-157 + TB-500 Regenerative Pairing')).toBeInTheDocument();
  });

  // T10a: Challenge panel non-executable label
  it('T10a — challenge panel contains non-executable counter-plan label', () => {
    renderBoard();
    expect(screen.getByTestId('non-executable-counter-plan')).toBeInTheDocument();
    expect(screen.getByTestId('non-executable-counter-plan')).toHaveTextContent(
      /non-executable/i,
    );
  });

  // T10b: Confidence profile sidebar rendered
  it('T10b — confidence profile sidebar is rendered when review is present', () => {
    renderBoard();
    const sidebar = screen.getByTestId('confidence-profile-sidebar');
    expect(sidebar).toBeInTheDocument();
    expect(sidebar).toHaveTextContent(/Confidence Profile/i);
    expect(sidebar).toHaveTextContent(/keon.collective-stub-v1/);
  });

  // T10c: Loading skeleton shown when loading=true
  it('T10c — shows loading skeleton when loading prop is true', () => {
    renderBoard({ loading: true });
    expect(screen.getByTestId('srb-loading-skeleton')).toBeInTheDocument();
    expect(screen.queryByText('Goal Alignment & Sequencing')).not.toBeInTheDocument();
  });

  // Null safety: renders without review
  it('renders gracefully when review is null', () => {
    renderBoard({ review: null });
    expect(screen.getByTestId('deterministic-safety-panel')).toBeInTheDocument();
    expect(screen.getByTestId('srb-divider')).toBeInTheDocument();
    expect(screen.queryByTestId('confidence-profile-sidebar')).not.toBeInTheDocument();
  });

  // Safety panel content
  it('renders deterministic finding content', () => {
    renderBoard();
    const safetyPanel = screen.getByTestId('deterministic-safety-panel');
    expect(within(safetyPanel).getByText('SAF-001')).toBeInTheDocument();
    expect(within(safetyPanel).getByText(/Injectable peptides require provider review/)).toBeInTheDocument();
  });
});
