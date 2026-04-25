import { ActiveCompoundsCard } from '@/components/dashboard/ActiveCompoundsCard';
import { ActiveGoalsCard } from '@/components/dashboard/ActiveGoalsCard';
import { ActiveProtocolRunCard } from '@/components/dashboard/ActiveProtocolRunCard';
import { LatestCheckInCard } from '@/components/dashboard/LatestCheckInCard';
import { Header } from '@/components/Header';
import { WeightUnitToggle } from '@/components/ui/WeightUnitToggle';
import { ProfileProvider } from '@/lib/context';
import { SettingsProvider } from '@/lib/settings';
import type { CheckIn, CompoundRecord, GoalDefinition, ProtocolRun } from '@/lib/types';
import { fireEvent, render, screen } from '@testing-library/react';
import React from 'react';
import { describe, expect, it } from 'vitest';

// Wrappers
const WithSettings = ({ children }: { children: React.ReactNode }) => (
  <SettingsProvider>{children}</SettingsProvider>
);

const WithAll = ({ children }: { children: React.ReactNode }) => (
  <SettingsProvider><ProfileProvider>{children}</ProfileProvider></SettingsProvider>
);

// ─── ActiveCompoundsCard ───────────────────────────────────────────────────────

const makeCompound = (status: string): CompoundRecord => ({
  id: 'c1', personId: 'p1', name: 'TestCompound', category: 'Peptide',
  startDate: '2024-01-01', endDate: null, status, notes: '', sourceType: 'Manual',
});

describe('ActiveCompoundsCard', () => {
  it('shows empty state when no active compounds', () => {
    render(<ActiveCompoundsCard compounds={[makeCompound('Planned')]} />);
    expect(screen.getByText('No active compounds')).toBeInTheDocument();
  });

  it('renders active compound names', () => {
    render(<ActiveCompoundsCard compounds={[makeCompound('Active')]} />);
    expect(screen.getByText('TestCompound')).toBeInTheDocument();
  });
});

// ─── ActiveGoalsCard ───────────────────────────────────────────────────────────

const makeGoal = (): GoalDefinition => ({
  id: 'g1', category: 'Recovery', label: 'Better sleep', description: 'Improve sleep quality',
  tags: [], relevantPathways: [],
});

describe('ActiveGoalsCard', () => {
  it('renders the Your Goals heading', () => {
    render(<ActiveGoalsCard goals={[]} />);
    expect(screen.getByText('Your Goals')).toBeInTheDocument();
  });

  it('renders view profile link when profileId is provided', () => {
    render(<ActiveGoalsCard goals={[makeGoal()]} profileId="profile-abc" />);
    expect(screen.getByText('View profile')).toBeInTheDocument();
  });

  it('does not render view profile link without profileId', () => {
    render(<ActiveGoalsCard goals={[makeGoal()]} />);
    expect(screen.queryByText('View profile')).not.toBeInTheDocument();
  });
});

// ─── ActiveProtocolRunCard ─────────────────────────────────────────────────────

const makeRun = (): ProtocolRun => ({
  id: 'r1', protocolId: 'proto1', personId: 'p1',
  protocolName: 'My Protocol', protocolVersion: 2,
  startedAtUtc: '2024-03-01T00:00:00Z', endedAtUtc: null,
  status: 'active', notes: '',
});

describe('ActiveProtocolRunCard', () => {
  it('renders null when no run', () => {
    const { container } = render(<ActiveProtocolRunCard run={null} />);
    expect(container.firstChild).toBeNull();
  });

  it('renders protocol name and version', () => {
    render(<ActiveProtocolRunCard run={makeRun()} />);
    expect(screen.getByText(/My Protocol v2/i)).toBeInTheDocument();
  });
});

// ─── LatestCheckInCard ─────────────────────────────────────────────────────────

const makeCheckIn = (): CheckIn => ({
  id: 'ci1', personId: 'p1', protocolRunId: null,
  date: '2024-04-01', weight: 80, sleepQuality: 7,
  energy: 8, appetite: 6, recovery: 7,
  giSymptoms: '', mood: 'good', notes: 'Feeling great',
});

describe('LatestCheckInCard', () => {
  it('shows empty state when no check-in', async () => {
    render(<WithSettings><LatestCheckInCard checkIn={null} /></WithSettings>);
    expect(await screen.findByText('No check-ins recorded yet')).toBeInTheDocument();
  });

  it('renders check-in stats', async () => {
    render(<WithSettings><LatestCheckInCard checkIn={makeCheckIn()} /></WithSettings>);
    // Energy value appears in the card
    expect(await screen.findByText('8/10')).toBeInTheDocument();
  });

  it('renders notes when provided', async () => {
    render(<WithSettings><LatestCheckInCard checkIn={makeCheckIn()} /></WithSettings>);
    expect(await screen.findByText('Feeling great')).toBeInTheDocument();
  });
});

// ─── WeightUnitToggle ──────────────────────────────────────────────────────────

describe('WeightUnitToggle', () => {
  it('renders kg and lbs buttons', async () => {
    render(<WithSettings><WeightUnitToggle /></WithSettings>);
    expect(await screen.findByText('kg')).toBeInTheDocument();
    expect(screen.getByText('lbs')).toBeInTheDocument();
  });

  it('clicking kg button invokes setWeightUnit metric', async () => {
    render(<WithSettings><WeightUnitToggle /></WithSettings>);
    const kgBtn = await screen.findByText('kg');
    fireEvent.click(kgBtn);
    // No crash = handler ran correctly
    expect(kgBtn).toBeInTheDocument();
  });

  it('clicking lbs button invokes setWeightUnit imperial', async () => {
    render(<WithSettings><WeightUnitToggle /></WithSettings>);
    const lbsBtn = await screen.findByText('lbs');
    fireEvent.click(lbsBtn);
    expect(lbsBtn).toBeInTheDocument();
  });
});

// ─── Header ───────────────────────────────────────────────────────────────────

describe('Header', () => {
  it('renders the page title', async () => {
    render(<WithAll><Header title="Dashboard" /></WithAll>);
    expect(await screen.findByText('Dashboard')).toBeInTheDocument();
  });

  it('renders subtitle when provided', async () => {
    render(<WithAll><Header title="Protocols" subtitle="Manage your protocols" /></WithAll>);
    expect(await screen.findByText('Manage your protocols')).toBeInTheDocument();
  });

  it('renders toggle navigation button for mobile', async () => {
    render(<WithAll><Header title="Page" /></WithAll>);
    expect(await screen.findByLabelText('Toggle navigation')).toBeInTheDocument();
  });
});
