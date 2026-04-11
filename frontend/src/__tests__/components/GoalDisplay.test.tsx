import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { GoalDisplay } from '@/components/goals/GoalDisplay';
import type { GoalDefinition } from '@/lib/types';

const goals: GoalDefinition[] = [
  { id: 'recovery-muscles', name: 'Repair muscles', category: 'recovery', description: '', isActive: true },
  { id: 'energy-levels', name: 'Improve energy levels', category: 'energy', description: '', isActive: true },
  { id: 'cognitive-focus', name: 'Improve focus', category: 'cognitive', description: '', isActive: true },
];

describe('GoalDisplay', () => {
  it('renders an empty state message when no goals are passed', () => {
    render(<GoalDisplay goals={[]} />);
    expect(screen.getByText(/no goals/i)).toBeInTheDocument();
  });

  it('renders all goal names', () => {
    render(<GoalDisplay goals={goals} />);
    expect(screen.getByText('Repair muscles')).toBeInTheDocument();
    expect(screen.getByText('Improve energy levels')).toBeInTheDocument();
    expect(screen.getByText('Improve focus')).toBeInTheDocument();
  });

  it('groups goals by category — shows category labels', () => {
    render(<GoalDisplay goals={goals} />);
    // Category labels are derived from getCategoryMeta()
    // Recovery & Repair, Energy & Metabolism, Cognitive & Neurological
    expect(screen.getByText('Recovery & Repair')).toBeInTheDocument();
    expect(screen.getByText('Energy & Metabolism')).toBeInTheDocument();
  });
});
