import { CohesionTimelinePanel } from '@/components/dashboard/CohesionTimelinePanel';
import { DriftRegimePanel } from '@/components/dashboard/DriftRegimePanel';
import { SequenceExpectationPanel } from '@/components/dashboard/SequenceExpectationPanel';
import type {
    ProtocolDriftSnapshot,
    ProtocolPatternSnapshot,
    ProtocolReviewTimelineEvent,
    ProtocolSequenceExpectationSnapshot
} from '@/lib/types';
import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';

// ─── DriftRegimePanel ──────────────────────────────────────────────────────────
// DriftRegimePanel exports: DriftRegimePanel, StatePill, baselineText,
// formatDriftState, formatSignalType, formatValue — all covered by these tests.

const makeDrift = (overrides: Partial<ProtocolDriftSnapshot> = {}): ProtocolDriftSnapshot => ({
  protocolId: 'proto1',
  driftState: 'none',
  baselineSource: 'insufficient_history',
  signals: [],
  regimeClassification: null,
  ...overrides,
});

const makePatterns = (): ProtocolPatternSnapshot => ({
  protocolId: 'proto1',
  historicalRunCount: 4,
  patternConfidence: 'moderate',
  metricPatterns: [],
  eventPatterns: [],
  sequencePatterns: [],
  currentRunComparison: null,
});

describe('DriftRegimePanel', () => {
  it('renders null state when drift is null', () => {
    render(<DriftRegimePanel drift={null} />);
    expect(screen.getByText('No drift snapshot')).toBeInTheDocument();
    expect(screen.getByText(/drift classification appears/i)).toBeInTheDocument();
  });

  it('renders with insufficient_history baseline', () => {
    render(<DriftRegimePanel drift={makeDrift({ baselineSource: 'insufficient_history' })} />);
    expect(screen.getByText('Insufficient historical run baseline')).toBeInTheDocument();
  });

  it('renders historical_runs baseline text with count from patterns', () => {
    render(<DriftRegimePanel drift={makeDrift({ baselineSource: 'historical_runs' })} patterns={makePatterns()} />);
    expect(screen.getByText(/baseline built from 4 prior runs/i)).toBeInTheDocument();
  });

  it('renders regime_shift as "shifted" in drift state pill', () => {
    render(<DriftRegimePanel drift={makeDrift({ driftState: 'regime_shift', baselineSource: 'insufficient_history' })} />);
    // formatDriftState converts 'regime_shift' → 'shifted'
    expect(screen.getByText('shifted')).toBeInTheDocument();
  });

  it('renders signals with formatted type', () => {
    const drift = makeDrift({
      signals: [{ type: 'checkin_timing', severity: 'mild', description: 'Check-ins are irregular.' }],
    });
    render(<DriftRegimePanel drift={drift} />);
    // formatSignalType converts underscores to spaces
    expect(screen.getByText('checkin timing')).toBeInTheDocument();
    expect(screen.getByText('Check-ins are irregular.')).toBeInTheDocument();
  });

  it('renders no drift signals text when signals array is empty', () => {
    render(<DriftRegimePanel drift={makeDrift()} />);
    expect(screen.getByText('No drift signals detected.')).toBeInTheDocument();
  });

  it('renders contributing factors when present', () => {
    const drift = makeDrift({
      regimeClassification: { state: 'drifting', contributingFactors: ['signal_density'] },
    });
    render(<DriftRegimePanel drift={drift} />);
    expect(screen.getByText('signal density')).toBeInTheDocument();
  });
});

// ─── SequenceExpectationPanel ──────────────────────────────────────────────────
// SequenceExpectationPanel exports: SequenceExpectationPanel, formatEvent,
// formatValue, statusClass — all covered by these tests.

const makeSeqSnapshot = (overrides: Partial<ProtocolSequenceExpectationSnapshot> = {}): ProtocolSequenceExpectationSnapshot => ({
  protocolId: 'proto1',
  baselineSource: 'historical_runs',
  historicalRunCount: 5,
  expectedNextEvent: null,
  commonTransitions: [],
  currentStatus: null,
  ...overrides,
});

describe('SequenceExpectationPanel', () => {
  it('renders null state when snapshot is null', () => {
    render(<SequenceExpectationPanel snapshot={null} />);
    expect(screen.getByText(/not enough sequence history/i)).toBeInTheDocument();
  });

  it('renders null state when baselineSource is insufficient_history', () => {
    render(<SequenceExpectationPanel snapshot={makeSeqSnapshot({ baselineSource: 'insufficient_history' })} />);
    expect(screen.getByText(/not enough sequence history/i)).toBeInTheDocument();
  });

  it('renders historical run count', () => {
    render(<SequenceExpectationPanel snapshot={makeSeqSnapshot()} />);
    expect(screen.getByText(/built from 5 completed runs/i)).toBeInTheDocument();
  });

  it('renders expectedNextEvent with formatEvent conversion', () => {
    const snapshot = makeSeqSnapshot({
      expectedNextEvent: {
        eventType: 'RunStarted',
        description: 'Expect a new run.',
        timingWindow: 'Days 1–7',
        confidence: 'moderate',
      },
    });
    render(<SequenceExpectationPanel snapshot={snapshot} />);
    // formatEvent converts 'RunStarted' → 'run start'
    expect(screen.getByText('run start')).toBeInTheDocument();
    expect(screen.getByText('Days 1–7')).toBeInTheDocument();
  });

  it('renders aligned status chip via statusClass', () => {
    const snapshot = makeSeqSnapshot({
      currentStatus: { state: 'aligned', notes: [] },
    });
    const { container } = render(<SequenceExpectationPanel snapshot={snapshot} />);
    // statusClass for 'aligned' adds emerald classes
    expect(container.querySelector('.text-emerald-100')).not.toBeNull();
  });

  it('renders late/diverging status chip', () => {
    const snapshot = makeSeqSnapshot({
      currentStatus: { state: 'late', notes: ['No recent check-in.'] },
    });
    const { container } = render(<SequenceExpectationPanel snapshot={snapshot} />);
    expect(container.querySelector('.text-amber-100')).not.toBeNull();
  });

  it('renders pending status chip', () => {
    const snapshot = makeSeqSnapshot({
      currentStatus: { state: 'pending', notes: [] },
    });
    const { container } = render(<SequenceExpectationPanel snapshot={snapshot} />);
    expect(container.querySelector('.text-cyan-100')).not.toBeNull();
  });

  it('renders fallback status chip for unknown state', () => {
    const snapshot = makeSeqSnapshot({
      currentStatus: { state: 'unknown', notes: [] },
    });
    const { container } = render(<SequenceExpectationPanel snapshot={snapshot} />);
    // statusClass default: text-white/55
    expect(container.querySelector('.text-white\\/55')).not.toBeNull();
  });
});


// ─── CohesionTimelinePanel ────────────────────────────────────────────────────
// Covers: CohesionTimelinePanel, formatDate, eventDotClass, eventBandClass,
// eventIcon, timelineBadges.

function makeEvent(overrides: Partial<ProtocolReviewTimelineEvent> = {}): ProtocolReviewTimelineEvent {
  return {
    occurredAtUtc: '2024-06-01T10:00:00Z',
    eventType: 'check_in',
    label: 'Check-in recorded',
    protocolId: 'proto1',
    runId: null,
    checkInId: 'ci1',
    computationId: null,
    reviewCompletedEventId: null,
    detail: '',
    ...overrides,
  };
}

describe('CohesionTimelinePanel', () => {
  it('renders empty state when events array is empty', () => {
    render(<CohesionTimelinePanel events={[]} />);
    expect(screen.getByText(/cohesion timeline/i)).toBeInTheDocument();
    expect(screen.getByText(/runs, check-ins/i)).toBeInTheDocument();
  });

  it('renders event label', () => {
    render(<CohesionTimelinePanel events={[makeEvent({ label: 'Day 3 check-in' })]} />);
    expect(screen.getByText('Day 3 check-in')).toBeInTheDocument();
  });

  it('renders check_in event with sky dot class', () => {
    const { container } = render(<CohesionTimelinePanel events={[makeEvent({ eventType: 'check_in' })]} />);
    expect(container.querySelector('.bg-sky-300')).not.toBeNull();
  });

  it('renders computation event with amber dot class', () => {
    const { container } = render(<CohesionTimelinePanel events={[makeEvent({ eventType: 'computation' })]} />);
    expect(container.querySelector('.bg-amber-300')).not.toBeNull();
  });

  it('renders run_ prefixed event with emerald dot class', () => {
    const { container } = render(<CohesionTimelinePanel events={[makeEvent({ eventType: 'run_started' })]} />);
    expect(container.querySelector('.bg-emerald-300')).not.toBeNull();
  });

  it('renders evolution event with fuchsia dot class', () => {
    const { container } = render(<CohesionTimelinePanel events={[makeEvent({ eventType: 'evolution' })]} />);
    expect(container.querySelector('.bg-fuchsia-300')).not.toBeNull();
  });

  it('renders review_completed event', () => {
    const { container } = render(<CohesionTimelinePanel events={[makeEvent({ eventType: 'review_completed' })]} />);
    expect(container.querySelector('.bg-lime-300')).not.toBeNull();
  });

  it('renders timelineBadges for "matches prior pattern" detail', () => {
    render(<CohesionTimelinePanel events={[makeEvent({ detail: 'matches prior pattern timing' })]} />);
    expect(screen.getByText('matches pattern')).toBeInTheDocument();
  });

  it('renders timelineBadges for "outside typical timing" detail', () => {
    render(<CohesionTimelinePanel events={[makeEvent({ detail: 'outside typical timing by 2 days' })]} />);
    expect(screen.getByText('outside typical timing')).toBeInTheDocument();
  });

  it('renders timelineBadges for sequence break detail', () => {
    render(<CohesionTimelinePanel events={[makeEvent({ detail: 'sequence diverging from expected' })]} />);
    expect(screen.getByText('sequence break')).toBeInTheDocument();
  });

  it('renders sequence snapshot status when provided', () => {
    render(
      <CohesionTimelinePanel
        events={[makeEvent()]}
        sequence={{ protocolId: 'p1', baselineSource: 'historical_runs', historicalRunCount: 3, expectedNextEvent: null, commonTransitions: [], currentStatus: { state: 'aligned', notes: [] } }}
      />
    );
    expect(screen.getByText(/current sequence status: aligned/i)).toBeInTheDocument();
  });
});
