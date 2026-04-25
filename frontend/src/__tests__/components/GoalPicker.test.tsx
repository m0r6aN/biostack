import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { GoalPicker } from '@/components/goals/GoalPicker';
import { GOAL_CATEGORIES, getGoalsByCategory } from '@/lib/goals';

describe('GoalPicker', () => {
  const noop = vi.fn();

  it('renders the Goals label', () => {
    render(<GoalPicker selectedGoalIds={[]} onChange={noop} />);
    expect(screen.getByText('Goals')).toBeInTheDocument();
  });

  it('displays the count of selected goals', () => {
    render(<GoalPicker selectedGoalIds={['goal-1', 'goal-2']} onChange={noop} />);
    expect(screen.getByText('2 selected')).toBeInTheDocument();
  });

  it('shows 0 selected when nothing is selected', () => {
    render(<GoalPicker selectedGoalIds={[]} onChange={noop} />);
    expect(screen.getByText('0 selected')).toBeInTheDocument();
  });

  it('renders all goal categories', () => {
    render(<GoalPicker selectedGoalIds={[]} onChange={noop} />);
    for (const cat of GOAL_CATEGORIES) {
      expect(screen.getByText(cat.label)).toBeInTheDocument();
    }
  });

  it('shows goals within categories by default (expanded)', () => {
    render(<GoalPicker selectedGoalIds={[]} onChange={noop} />);
    // Categories are expanded by default; at least one goal button should be present
    const allGoals = Array.from(getGoalsByCategory().values()).flat();
    const firstGoal = allGoals[0];
    expect(screen.getByText(firstGoal.name)).toBeInTheDocument();
  });

  it('collapses and expands a category on button click', () => {
    render(<GoalPicker selectedGoalIds={[]} onChange={noop} />);

    const firstCategory = GOAL_CATEGORIES[0];
    const goals = getGoalsByCategory().get(firstCategory.key) ?? [];
    const firstGoalName = goals[0]?.name ?? '';

    // Before collapse: goal is visible
    expect(screen.getByText(firstGoalName)).toBeInTheDocument();

    // Click the category header to collapse
    fireEvent.click(screen.getByText(firstCategory.label));

    // After collapse: goal should no longer be visible
    expect(screen.queryByText(firstGoalName)).not.toBeInTheDocument();

    // Click again to expand
    fireEvent.click(screen.getByText(firstCategory.label));
    expect(screen.getByText(firstGoalName)).toBeInTheDocument();
  });

  it('calls onChange with goal added when an unselected goal is clicked', () => {
    const onChange = vi.fn();
    const allGoals = Array.from(getGoalsByCategory().values()).flat();
    const target = allGoals[0];

    render(<GoalPicker selectedGoalIds={[]} onChange={onChange} />);
    fireEvent.click(screen.getByText(target.name));

    expect(onChange).toHaveBeenCalledWith([target.id]);
  });

  it('calls onChange with goal removed when an already-selected goal is clicked', () => {
    const onChange = vi.fn();
    const allGoals = Array.from(getGoalsByCategory().values()).flat();
    const target = allGoals[0];

    render(<GoalPicker selectedGoalIds={[target.id]} onChange={onChange} />);
    fireEvent.click(screen.getByText(target.name));

    expect(onChange).toHaveBeenCalledWith([]);
  });

  it('shows selected count badge within category when goals are selected', () => {
    const allGoals = Array.from(getGoalsByCategory().values()).flat();
    const target = allGoals[0];

    render(<GoalPicker selectedGoalIds={[target.id]} onChange={noop} />);
    expect(screen.getByText('1')).toBeInTheDocument();
  });

  it('does not render custom goal textarea when onCustomGoalNoteChange is not provided', () => {
    render(<GoalPicker selectedGoalIds={[]} onChange={noop} />);
    expect(screen.queryByPlaceholderText(/anything else/i)).not.toBeInTheDocument();
  });

  it('renders custom goal textarea when onCustomGoalNoteChange is provided', () => {
    render(
      <GoalPicker
        selectedGoalIds={[]}
        onChange={noop}
        customGoalNote=""
        onCustomGoalNoteChange={vi.fn()}
      />
    );
    expect(screen.getByPlaceholderText(/anything else/i)).toBeInTheDocument();
  });

  it('calls onCustomGoalNoteChange when textarea value changes', () => {
    const onCustomGoalNoteChange = vi.fn();
    render(
      <GoalPicker
        selectedGoalIds={[]}
        onChange={noop}
        customGoalNote=""
        onCustomGoalNoteChange={onCustomGoalNoteChange}
      />
    );
    const textarea = screen.getByPlaceholderText(/anything else/i);
    fireEvent.change(textarea, { target: { value: 'Boost immune system' } });
    expect(onCustomGoalNoteChange).toHaveBeenCalledWith('Boost immune system');
  });
});
