import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, act } from '@testing-library/react';
import React from 'react';
import { GlassPanel } from '@/components/ui/GlassPanel';
import { SimulationTimeline } from '@/components/protocols/SimulationTimeline';
import type { SimulationResult } from '@/lib/types';
import { ProfileProvider, useProfile } from '@/lib/context';

// ─── GlassPanel ──────────────────────────────────────────────────────────────

describe('GlassPanel', () => {
  it('renders children', () => {
    render(<GlassPanel><span>Content</span></GlassPanel>);
    expect(screen.getByText('Content')).toBeInTheDocument();
  });

  it('applies medium intensity class by default', () => {
    const { container } = render(<GlassPanel>Panel</GlassPanel>);
    expect(container.firstChild).toHaveClass('glass-medium');
  });

  it('applies light intensity class', () => {
    const { container } = render(<GlassPanel intensity="light">Panel</GlassPanel>);
    expect(container.firstChild).toHaveClass('glass-light');
  });

  it('applies strong intensity class', () => {
    const { container } = render(<GlassPanel intensity="strong">Panel</GlassPanel>);
    expect(container.firstChild).toHaveClass('glass-strong');
  });

  it('merges custom className', () => {
    const { container } = render(<GlassPanel className="my-panel">Panel</GlassPanel>);
    expect(container.firstChild).toHaveClass('my-panel');
    expect(container.firstChild).toHaveClass('glass-medium');
  });

  it('forwards extra div props', () => {
    render(<GlassPanel aria-label="hero panel">Panel</GlassPanel>);
    expect(screen.getByLabelText('hero panel')).toBeInTheDocument();
  });
});

// ─── SimulationTimeline ───────────────────────────────────────────────────────

const makeSimulation = (overrides: Partial<SimulationResult> = {}): SimulationResult => ({
  timeline: [],
  insights: [],
  ...overrides,
});

describe('SimulationTimeline', () => {
  it('renders the heading', () => {
    render(<SimulationTimeline simulation={makeSimulation()} />);
    expect(screen.getByText(/projected observation timeline/i)).toBeInTheDocument();
  });

  it('renders timeline entries', () => {
    render(<SimulationTimeline simulation={makeSimulation({
      timeline: [
        { dayRange: '1-3', signals: ['Initial adaptation phase.', 'Expect mild fatigue.'] },
        { dayRange: '4-7', signals: ['Tissue repair accelerates.'] },
      ],
    })} />);
    expect(screen.getByText('Day 1-3')).toBeInTheDocument();
    expect(screen.getByText('Initial adaptation phase.')).toBeInTheDocument();
    expect(screen.getByText('Day 4-7')).toBeInTheDocument();
    expect(screen.getByText('Tissue repair accelerates.')).toBeInTheDocument();
  });

  it('does not render Key insights section when insights are empty', () => {
    render(<SimulationTimeline simulation={makeSimulation()} />);
    expect(screen.queryByText(/key insights/i)).not.toBeInTheDocument();
  });

  it('renders Key insights section when insights are provided', () => {
    render(<SimulationTimeline simulation={makeSimulation({
      insights: ['Strong synergy across tissue repair pathways.'],
    })} />);
    expect(screen.getByText(/key insights/i)).toBeInTheDocument();
    expect(screen.getByText('Strong synergy across tissue repair pathways.')).toBeInTheDocument();
  });

  it('renders multiple timeline entries without crashing', () => {
    const timeline = Array.from({ length: 5 }, (_, i) => ({
      dayRange: `${i * 7 + 1}-${i * 7 + 7}`,
      signals: [`Signal for week ${i + 1}`],
    }));
    expect(() => render(<SimulationTimeline simulation={makeSimulation({ timeline })} />)).not.toThrow();
  });
});

// ─── ProfileProvider / useProfile ────────────────────────────────────────────

function ProfileConsumer() {
  const { currentProfileId, setCurrentProfileId, profiles, isSidebarOpen, setSidebarOpen } = useProfile();
  return (
    <div>
      <span data-testid="profileId">{currentProfileId ?? 'none'}</span>
      <span data-testid="profileCount">{profiles.length}</span>
      <span data-testid="sidebar">{isSidebarOpen ? 'open' : 'closed'}</span>
      <button onClick={() => setCurrentProfileId('profile-123')}>Set Profile</button>
      <button onClick={() => setSidebarOpen(true)}>Open Sidebar</button>
    </div>
  );
}

describe('ProfileProvider', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it('renders children without crashing', () => {
    render(<ProfileProvider><span>Child</span></ProfileProvider>);
    expect(screen.getByText('Child')).toBeInTheDocument();
  });

  it('provides null currentProfileId by default', async () => {
    render(<ProfileProvider><ProfileConsumer /></ProfileProvider>);
    expect(await screen.findByTestId('profileId')).toHaveTextContent('none');
  });

  it('reads currentProfileId from localStorage on mount', async () => {
    localStorage.setItem('currentProfileId', 'profile-saved');
    render(<ProfileProvider><ProfileConsumer /></ProfileProvider>);
    const el = await screen.findByTestId('profileId');
    expect(el.textContent).toBe('profile-saved');
  });

  it('allows updating currentProfileId', async () => {
    render(<ProfileProvider><ProfileConsumer /></ProfileProvider>);
    await act(async () => {
      screen.getByText('Set Profile').click();
    });
    expect(screen.getByTestId('profileId').textContent).toBe('profile-123');
  });

  it('allows toggling sidebar open', async () => {
    render(<ProfileProvider><ProfileConsumer /></ProfileProvider>);
    expect(screen.getByTestId('sidebar').textContent).toBe('closed');
    await act(async () => {
      screen.getByText('Open Sidebar').click();
    });
    expect(screen.getByTestId('sidebar').textContent).toBe('open');
  });
});

describe('useProfile error case', () => {
  it('throws when used outside ProfileProvider', () => {
    function Broken() {
      useProfile();
      return null;
    }
    // Suppress React's error boundary output
    const spy = vi.spyOn(console, 'error').mockImplementation(() => {});
    expect(() => render(<Broken />)).toThrow('useProfile must be used within a ProfileProvider');
    spy.mockRestore();
  });
});
