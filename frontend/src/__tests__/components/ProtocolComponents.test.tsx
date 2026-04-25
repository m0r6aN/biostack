import { ProtocolComparison } from '@/components/protocols/ProtocolComparison';
import { StackScoreCard } from '@/components/protocols/StackScoreCard';
import type { ProtocolActualComparison, StackScore } from '@/lib/types';
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
