import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { GoalBadge } from '@/components/goals/GoalBadge';
import type { GoalDefinition } from '@/lib/types';

const mockGoal: GoalDefinition = {
  id: 'recovery-muscles',
  name: 'Repair muscles, joints, and tendons',
  category: 'recovery',
  description: 'Support structural tissue recovery',
  isActive: true,
};

describe('GoalBadge', () => {
  it('renders the goal name', () => {
    render(<GoalBadge goal={mockGoal} />);
    expect(screen.getByText('Repair muscles, joints, and tendons')).toBeInTheDocument();
  });

  it('does not show a remove button when onRemove is not provided', () => {
    render(<GoalBadge goal={mockGoal} />);
    expect(screen.queryByRole('button')).toBeNull();
  });

  it('shows a remove button when onRemove is provided', () => {
    const onRemove = vi.fn();
    render(<GoalBadge goal={mockGoal} onRemove={onRemove} />);
    expect(screen.getByRole('button')).toBeInTheDocument();
  });

  it('calls onRemove when the remove button is clicked', () => {
    const onRemove = vi.fn();
    render(<GoalBadge goal={mockGoal} onRemove={onRemove} />);
    fireEvent.click(screen.getByRole('button'));
    expect(onRemove).toHaveBeenCalledOnce();
  });

  it('renders with compact prop without error', () => {
    expect(() => render(<GoalBadge goal={mockGoal} compact />)).not.toThrow();
  });
});
