import { InteractionIntelligenceCard } from '@/components/protocols/InteractionIntelligenceCard';
import { ProtocolComparison } from '@/components/protocols/ProtocolComparison';
import { ProtocolContinuityStrip } from '@/components/protocols/ProtocolContinuityStrip';
import { StackScoreCard } from '@/components/protocols/StackScoreCard';
import type { InteractionIntelligence, Protocol, ProtocolActualComparison, ProtocolReview, StackScore } from '@/lib/types';
import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';

const makeStackScore = (overrides: Partial<StackScore> = {}): StackScore => ({
  score: 78,
  breakdown: { synergy: 25, redundancy: 10, conflicts: 5, evidence: 38 },
  chips: ['Synergistic Stack', 'Recovery Optimized'],
  ...overrides,
});

describe('StackScoreCard', () => {
  it('renders the stack score value', () => {
    render(<StackScoreCard score={makeStackScore()} />);
    expect(screen.getByText('78')).toBeInTheDocument();
  });

  it('renders the Stack Score label', () => {
    render(<StackScoreCard score={makeStackScore()} />);
    expect(screen.getByText(/stack score/i)).toBeInTheDocument();
  });

  it('renders chips', () => {
    render(<StackScoreCard score={makeStackScore()} />);
    expect(screen.getByText('Synergistic Stack')).toBeInTheDocument();
    expect(screen.getByText('Recovery Optimized')).toBeInTheDocument();
  });

  it('renders breakdown labels', () => {
    render(<StackScoreCard score={makeStackScore()} />);
    expect(screen.getByText('Synergy')).toBeInTheDocument();
    expect(screen.getByText('Redundancy')).toBeInTheDocument();
    expect(screen.getByText('Conflicts')).toBeInTheDocument();
    expect(screen.getByText('Evidence')).toBeInTheDocument();
  });

  it('renders breakdown values', () => {
    render(<StackScoreCard score={makeStackScore()} />);
    expect(screen.getByText('25')).toBeInTheDocument();
    expect(screen.getByText('10')).toBeInTheDocument();
    expect(screen.getByText('5')).toBeInTheDocument();
    expect(screen.getByText('38')).toBeInTheDocument();
  });

  it('renders with score 0 without crashing', () => {
    expect(() =>
      render(<StackScoreCard score={makeStackScore({ score: 0, chips: [] })} />)
    ).not.toThrow();
  });

  it('renders with no chips without crashing', () => {
    render(<StackScoreCard score={makeStackScore({ chips: [] })} />);
    expect(screen.getByText('78')).toBeInTheDocument();
  });
});

const makeComparison = (overrides: Partial<ProtocolActualComparison> = {}): ProtocolActualComparison => ({
  simulation: { timeline: [], insights: [] },
  run: null,
  runSummary: null,
  observations: [],
  actualTrends: [],
  insights: [],
  highlights: [],
  ...overrides,
});

describe('ProtocolComparison', () => {
  it('renders empty state when comparison is null', () => {
    render(<ProtocolComparison comparison={null} />);
    expect(screen.getByText(/simulate or start your first run/i)).toBeInTheDocument();
  });

  it('renders run intelligence heading when comparison is provided', () => {
    render(<ProtocolComparison comparison={makeComparison()} />);
    expect(screen.getByText('Run intelligence')).toBeInTheDocument();
  });

  it('shows no-observations message when run exists but no observations', () => {
    render(<ProtocolComparison comparison={makeComparison({
      run: {
        id: 'run-1', protocolId: 'p-1', personId: 'u-1',
        protocolName: 'Test', protocolVersion: 1,
        startedAtUtc: '2025-01-01T00:00:00Z', endedAtUtc: null,
        status: 'active', notes: '',
      },
    })} />);
    expect(screen.getByText(/no observations yet/i)).toBeInTheDocument();
  });

  it('renders trend cards when actualTrends are present', () => {
    render(<ProtocolComparison comparison={makeComparison({
      actualTrends: [
        { metric: 'Energy', beforeAverage: 5, afterAverage: 8, direction: 'up' },
      ],
    })} />);
    // 'Energy' appears in both legend and trend card — use getAllByText
    expect(screen.getAllByText('Energy').length).toBeGreaterThan(0);
    expect(screen.getByText('up')).toBeInTheDocument();
  });

  it('renders highlights when provided', () => {
    render(<ProtocolComparison comparison={makeComparison({
      highlights: ['Energy improved significantly.'],
    })} />);
    expect(screen.getByText('Energy improved significantly.')).toBeInTheDocument();
  });

  it('renders insights when provided', () => {
    render(<ProtocolComparison comparison={makeComparison({
      insights: [{ type: 'alignment', message: 'Sleep aligned with projection.', relatedSignals: [] }],
    })} />);
    expect(screen.getByText('Sleep aligned with projection.')).toBeInTheDocument();
  });

  it('renders the metric color legend', () => {
    render(<ProtocolComparison comparison={makeComparison()} />);
    expect(screen.getByText('Energy')).toBeInTheDocument();
    expect(screen.getByText('Sleep')).toBeInTheDocument();
  });
});

const makeProtocol = (overrides: Partial<Protocol> = {}): Protocol => ({
  id: 'protocol-2',
  personId: 'person-1',
  name: 'Recovery Protocol',
  version: 2,
  parentProtocolId: 'protocol-1',
  originProtocolId: 'protocol-1',
  evolvedFromRunId: null,
  isDraft: true,
  evolutionContext: '',
  isCurrentVersion: true,
  priorVersions: [{ id: 'protocol-1', name: 'Original Protocol', version: 1, isDraft: false, createdAtUtc: '2026-01-01T00:00:00Z' }],
  createdAtUtc: '2026-01-02T00:00:00Z',
  updatedAtUtc: '2026-01-02T00:00:00Z',
  items: [],
  stackScore: makeStackScore(),
  simulation: { timeline: [], insights: [] },
  interactionIntelligence: {
    summary: { synergies: 0, redundancies: 0, interferences: 0 },
    score: { synergyScore: 0, redundancyPenalty: 0, interferencePenalty: 0 },
    compositeScore: 0,
    topFindings: [],
    interactions: [],
    counterfactuals: [],
    swaps: [],
  },
  activeRun: null,
  versionDiff: {
    fromProtocolId: 'protocol-1',
    toProtocolId: 'protocol-2',
    changes: [{ changeType: 'added', scope: 'compound', subject: 'BPC-157', before: '', after: 'BPC-157' }],
  },
  actualComparison: null,
  ...overrides,
});

const makeReview = (): ProtocolReview => ({
  lineageRootProtocolId: 'protocol-1',
  requestedProtocolId: 'protocol-2',
  lineageName: 'Recovery Protocol',
  versions: [
    {
      protocolId: 'protocol-2',
      name: 'Recovery Protocol',
      version: 2,
      isDraft: true,
      parentProtocolId: 'protocol-1',
      evolvedFromRunId: null,
      evolutionContext: '',
      createdAtUtc: '2026-01-02T00:00:00Z',
      versionDiff: null,
      runs: [
        {
          run: {
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
          summary: null,
          observations: [],
          trends: [],
          insights: [],
        },
      ],
    },
  ],
  sections: [{ type: 'alignment', title: 'Alignment', summary: 'Energy matched the expected sequence.', evidence: [] }],
  timeline: [],
  safetyNotes: [],
});

describe('ProtocolContinuityStrip', () => {
  it('renders lineage, run state, review signal, and intelligence summaries', () => {
    render(
      <ProtocolContinuityStrip
        protocol={makeProtocol()}
        review={makeReview()}
        patterns={{
          protocolId: 'protocol-2',
          historicalRunCount: 2,
          patternConfidence: 'moderate',
          metricPatterns: [{ metric: 'Check-in cadence', observation: 'Check-ins usually arrive every other day.' }],
          eventPatterns: [],
          sequencePatterns: [],
          currentRunComparison: { similarity: 'moderate', matchingSignals: ['Cadence is aligned.'], divergentSignals: [] },
        }}
        drift={{
          protocolId: 'protocol-2',
          driftState: 'mild',
          baselineSource: 'historical_runs',
          signals: [{ type: 'checkin_timing', severity: 'mild', description: 'Check-ins are slightly later.' }],
          regimeClassification: { state: 'drifting', contributingFactors: ['check-in timing'] },
        }}
        sequence={{
          protocolId: 'protocol-2',
          baselineSource: 'historical_runs',
          historicalRunCount: 2,
          expectedNextEvent: {
            eventType: 'FirstCheckIn',
            description: 'First check-in usually follows run start.',
            timingWindow: 'within 48 hours',
            confidence: 'moderate',
          },
          commonTransitions: [],
          currentStatus: { state: 'pending', notes: ['Waiting for first check-in.'] },
        }}
      />
    );

    expect(screen.getByText('v2 draft follows v1')).toBeInTheDocument();
    expect(screen.getByText('Original Protocol')).toBeInTheDocument();
    expect(screen.getByText('active')).toBeInTheDocument();
    expect(screen.getByText('Energy matched the expected sequence.')).toBeInTheDocument();
    expect(screen.getByText('drifting')).toBeInTheDocument();
    expect(screen.getByText('1 item')).toBeInTheDocument();
    expect(screen.getByText('2 runs')).toBeInTheDocument();
    expect(screen.getByText('first check-in')).toBeInTheDocument();
    expect(screen.getByText('pending')).toBeInTheDocument();
  });
});

const makeIntelligence = (overrides: Partial<InteractionIntelligence> = {}): InteractionIntelligence => ({
  summary: { synergies: 1, redundancies: 0, interferences: 0 },
  score: { synergyScore: 0.8, redundancyPenalty: 0, interferencePenalty: 0 },
  compositeScore: 82,
  topFindings: [],
  interactions: [],
  counterfactuals: [],
  swaps: [],
  ...overrides,
});

describe('InteractionIntelligenceCard', () => {
  it('applies the teal tone to Complementary findings', () => {
    render(
      <InteractionIntelligenceCard
        intelligence={makeIntelligence({
          topFindings: [
            {
              type: 'Complementary',
              compounds: ['BPC-157', 'TB-500'],
              message: 'Distinct mechanisms converging on tissue repair.',
              confidence: 0.86,
            },
          ],
        })}
      />
    );

    const badge = screen.getByText('complementary');
    expect(badge.className).toContain('border-teal-400/20');
    expect(badge.className).toContain('bg-teal-500/10');
    expect(badge.className).toContain('text-teal-100');
    expect(screen.getByText('BPC-157 + TB-500')).toBeInTheDocument();
  });
});
