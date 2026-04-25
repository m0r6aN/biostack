import { describe, it, expect } from 'vitest';
import { render, screen, act } from '@testing-library/react';
import React from 'react';
import { SettingsProvider } from '@/lib/settings';
import { ProfileProvider } from '@/lib/context';
import { ObservationSignalsPanel } from '@/components/dashboard/ObservationSignalsPanel';
import { PatternMemoryPanel } from '@/components/dashboard/PatternMemoryPanel';
import { TimelineSnapshot } from '@/components/dashboard/TimelineSnapshot';
import { MarketingFooter } from '@/components/marketing/MarketingFooter';
import { ProfileSwitcher } from '@/components/ProfileSwitcher';
import type {
  ProtocolConsoleObservationSignal,
  ProtocolPatternSnapshot,
  TimelineEvent,
} from '@/lib/types';

const WithAll = ({ children }: { children: React.ReactNode }) => (
  <SettingsProvider><ProfileProvider>{children}</ProfileProvider></SettingsProvider>
);

// ─── ObservationSignalsPanel ───────────────────────────────────────────────────

const makeSignal = (overrides: Partial<ProtocolConsoleObservationSignal> = {}): ProtocolConsoleObservationSignal => ({
  type: 'trend_shift',
  metric: 'Energy',
  detail: 'Energy declined over last 3 check-ins.',
  severity: 'medium',
  ...overrides,
});

describe('ObservationSignalsPanel', () => {
  it('renders zero-state when no signals', () => {
    render(<ObservationSignalsPanel signals={[]} />);
    expect(screen.getByText(/no observations recorded/i)).toBeInTheDocument();
  });

  it('renders signal count in heading', () => {
    render(<ObservationSignalsPanel signals={[makeSignal()]} />);
    expect(screen.getByText('1 current signal')).toBeInTheDocument();
  });

  it('renders plural heading for multiple signals', () => {
    render(<ObservationSignalsPanel signals={[makeSignal(), makeSignal({ metric: 'Sleep', detail: 'Sleep declining.' })]} />);
    expect(screen.getByText('2 current signals')).toBeInTheDocument();
  });

  it('renders "Observation gap" label for gap type signals', () => {
    render(<ObservationSignalsPanel signals={[makeSignal({ type: 'gap', metric: null })]} />);
    expect(screen.getByText('Observation gap')).toBeInTheDocument();
  });

  it('renders severity dot for high severity', () => {
    const { container } = render(<ObservationSignalsPanel signals={[makeSignal({ severity: 'high' })]} />);
    expect(container.querySelector('.bg-red-300')).not.toBeNull();
  });

  it('renders sky dot for low severity', () => {
    const { container } = render(<ObservationSignalsPanel signals={[makeSignal({ severity: 'low' })]} />);
    expect(container.querySelector('.bg-sky-300')).not.toBeNull();
  });
});

// ─── PatternMemoryPanel ────────────────────────────────────────────────────────

const makeSnapshot = (): ProtocolPatternSnapshot => ({
  protocolId: 'proto1',
  historicalRunCount: 3,
  patternConfidence: 'moderate',
  metricPatterns: [{ metric: 'Energy', observation: 'Energy improves in weeks 2-4.' }],
  eventPatterns: [],
  sequencePatterns: [],
  currentRunComparison: null,
});

describe('PatternMemoryPanel', () => {
  it('renders empty state when snapshot is null', () => {
    render(<PatternMemoryPanel snapshot={null} />);
    expect(screen.getByText(/pattern memory/i)).toBeInTheDocument();
    expect(screen.getByText(/historical run recall appears/i)).toBeInTheDocument();
  });

  it('renders historical run count', () => {
    render(<PatternMemoryPanel snapshot={makeSnapshot()} />);
    expect(screen.getByText(/built from 3 completed runs/i)).toBeInTheDocument();
  });

  it('renders metric patterns', () => {
    render(<PatternMemoryPanel snapshot={makeSnapshot()} />);
    expect(screen.getByText('Energy improves in weeks 2-4.')).toBeInTheDocument();
  });
});

// ─── TimelineSnapshot ──────────────────────────────────────────────────────────

const makeEvent = (): TimelineEvent => ({
  id: 'e1', personId: 'p1', eventType: 'check_in',
  title: 'Check-in recorded', description: 'Weekly check-in logged.',
  occurredAtUtc: '2024-04-01T10:00:00Z',
  relatedEntityId: null, relatedEntityType: null,
});

describe('TimelineSnapshot', () => {
  it('renders empty state when no events', () => {
    render(<TimelineSnapshot events={[]} />);
    expect(screen.getByText('No events yet')).toBeInTheDocument();
  });

  it('renders event titles', () => {
    render(<TimelineSnapshot events={[makeEvent()]} />);
    expect(screen.getByText('Check-in recorded')).toBeInTheDocument();
  });

  it('renders heading', () => {
    render(<TimelineSnapshot events={[]} />);
    expect(screen.getByText('Recent Events')).toBeInTheDocument();
  });
});

// ─── MarketingFooter ───────────────────────────────────────────────────────────

describe('MarketingFooter', () => {
  it('renders footer content', () => {
    render(<MarketingFooter />);
    expect(screen.getByText(/BioStack/i)).toBeInTheDocument();
  });

  it('renders navigation links', () => {
    render(<MarketingFooter />);
    expect(screen.getByText('How it works')).toBeInTheDocument();
    expect(screen.getByText('Privacy')).toBeInTheDocument();
  });
});

// ─── ProfileSwitcher ───────────────────────────────────────────────────────────

describe('ProfileSwitcher', () => {
  it('renders the select profile button', async () => {
    render(<WithAll><ProfileSwitcher /></WithAll>);
    expect(await screen.findByText('Select Profile')).toBeInTheDocument();
  });
});
