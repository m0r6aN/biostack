import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { TimelineEventCard } from '@/components/timeline/TimelineEventCard';
import { TimelineFilter } from '@/components/timeline/TimelineFilter';
import type { TimelineEvent } from '@/lib/types';

const makeEvent = (overrides: Partial<TimelineEvent> = {}): TimelineEvent => ({
  id: 'evt-001',
  personId: 'person-001',
  eventType: 'compound_added',
  title: 'Added BPC-157',
  description: 'BPC-157 500mcg daily was added to your stack.',
  occurredAtUtc: '2025-01-15T10:30:00Z',
  relatedEntityId: null,
  relatedEntityType: null,
  ...overrides,
});

describe('TimelineEventCard', () => {
  it('renders the event title', () => {
    render(<TimelineEventCard event={makeEvent()} />);
    expect(screen.getByText('Added BPC-157')).toBeInTheDocument();
  });

  it('renders the event description', () => {
    render(<TimelineEventCard event={makeEvent()} />);
    expect(screen.getByText('BPC-157 500mcg daily was added to your stack.')).toBeInTheDocument();
  });

  it('renders a formatted date', () => {
    render(<TimelineEventCard event={makeEvent({ occurredAtUtc: '2025-01-15T10:30:00Z' })} />);
    // The component calls formatDateTime which produces a localized date string
    // Just ensure some date-like text is rendered
    const el = screen.getByText(/jan/i);
    expect(el).toBeInTheDocument();
  });

  it('renders an event icon for compound_added events', () => {
    render(<TimelineEventCard event={makeEvent({ eventType: 'compound_added' })} />);
    expect(screen.getByText('➕')).toBeInTheDocument();
  });

  it('renders an event icon for compound_ended events', () => {
    render(<TimelineEventCard event={makeEvent({ eventType: 'compound_ended' })} />);
    expect(screen.getByText('🛑')).toBeInTheDocument();
  });

  it('renders for check_in events without crashing', () => {
    expect(() =>
      render(<TimelineEventCard event={makeEvent({ eventType: 'check_in', title: 'Weekly Check-in' })} />)
    ).not.toThrow();
    expect(screen.getByText('Weekly Check-in')).toBeInTheDocument();
  });

  it('renders for phase_started events without crashing', () => {
    expect(() =>
      render(<TimelineEventCard event={makeEvent({ eventType: 'phase_started', title: 'Phase 1 started' })} />)
    ).not.toThrow();
  });

  it('renders for knowledge_update events without crashing', () => {
    expect(() =>
      render(<TimelineEventCard event={makeEvent({ eventType: 'knowledge_update', title: 'Knowledge updated' })} />)
    ).not.toThrow();
  });
});

describe('TimelineFilter', () => {
  const filters = [
    { id: 'all', label: 'All Events' },
    { id: 'compound_added', label: 'Compounds Added' },
    { id: 'compound_ended', label: 'Compounds Ended' },
    { id: 'phase_started', label: 'Phases Started' },
    { id: 'check_in', label: 'Check-ins' },
    { id: 'knowledge_update', label: 'Knowledge Updates' },
  ];

  it('renders all filter buttons', () => {
    render(<TimelineFilter activeFilter="all" onFilterChange={vi.fn()} />);
    for (const filter of filters) {
      expect(screen.getByText(filter.label)).toBeInTheDocument();
    }
  });

  it('marks the active filter with an active style class', () => {
    render(<TimelineFilter activeFilter="check_in" onFilterChange={vi.fn()} />);
    const activeBtn = screen.getByText('Check-ins');
    expect(activeBtn.className).toContain('bg-emerald-500/10');
  });

  it('marks the inactive filters with inactive style', () => {
    render(<TimelineFilter activeFilter="all" onFilterChange={vi.fn()} />);
    const inactiveBtn = screen.getByText('Compounds Added');
    expect(inactiveBtn.className).toContain('bg-white/[0.04]');
  });

  it('calls onFilterChange when a filter button is clicked', () => {
    const onFilterChange = vi.fn();
    render(<TimelineFilter activeFilter="all" onFilterChange={onFilterChange} />);
    fireEvent.click(screen.getByText('Check-ins'));
    expect(onFilterChange).toHaveBeenCalledWith('check_in');
  });

  it('calls onFilterChange with "all" when the all-events button is clicked', () => {
    const onFilterChange = vi.fn();
    render(<TimelineFilter activeFilter="check_in" onFilterChange={onFilterChange} />);
    fireEvent.click(screen.getByText('All Events'));
    expect(onFilterChange).toHaveBeenCalledWith('all');
  });
});
