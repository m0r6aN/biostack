import { ProfileForm } from '@/components/profiles/ProfileForm';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('@/components/goals/GoalPicker', () => ({
  GoalPicker: ({
    customGoalNote,
    onChange,
    onCustomGoalNoteChange,
  }: {
    customGoalNote: string;
    onChange: (goalIds: string[]) => void;
    onCustomGoalNoteChange: (value: string) => void;
  }) => (
    <div>
      <label htmlFor="goal-summary">Goal summary</label>
      <input
        id="goal-summary"
        value={customGoalNote}
        onChange={(event) => onCustomGoalNoteChange(event.target.value)}
      />
      <button type="button" onClick={() => onChange(['recovery'])}>
        Select recovery goal
      </button>
    </div>
  ),
}));

vi.mock('@/components/ui/WeightUnitToggle', () => ({
  WeightUnitToggle: () => <button type="button">kg/lb</button>,
}));

const settings = { weightUnit: 'metric' };

vi.mock('@/lib/settings', () => ({
  useSettings: () => ({ settings }),
}));

describe('ProfileForm', () => {
  beforeEach(() => {
    settings.weightUnit = 'metric';
  });

  it('submits profile fields with selected goals and goal summary', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined);

    render(<ProfileForm onSubmit={onSubmit} onCancel={vi.fn()} />);

    fireEvent.change(screen.getByLabelText('Display Name'), { target: { value: 'Training Profile' } });
    fireEvent.change(screen.getByLabelText('Sex'), { target: { value: 'Female' } });
    fireEvent.change(screen.getByLabelText('Age'), { target: { value: '34' } });
    fireEvent.change(screen.getByLabelText('Weight'), { target: { value: '72.5' } });
    fireEvent.change(screen.getByLabelText('Goal summary'), { target: { value: 'Improve recovery' } });
    fireEvent.click(screen.getByRole('button', { name: 'Select recovery goal' }));
    fireEvent.change(screen.getByLabelText('Notes'), { target: { value: 'No evening stimulants.' } });
    fireEvent.click(screen.getByRole('button', { name: 'Create Profile' }));

    await waitFor(() => expect(onSubmit).toHaveBeenCalledTimes(1));
    expect(onSubmit).toHaveBeenCalledWith(expect.objectContaining({
      displayName: 'Training Profile',
      sex: 'Female',
      age: 34,
      weight: 72.5,
      notes: 'No evening stimulants.',
      goalSummary: 'Improve recovery',
      selectedGoalIds: ['recovery'],
    }));
  });

  it('converts imperial weight input to kilograms before submit', async () => {
    settings.weightUnit = 'imperial';
    const onSubmit = vi.fn().mockResolvedValue(undefined);

    render(<ProfileForm onSubmit={onSubmit} onCancel={vi.fn()} />);

    fireEvent.change(screen.getByLabelText('Display Name'), { target: { value: 'Imperial Profile' } });
    fireEvent.change(screen.getByLabelText('Weight'), { target: { value: '220.46' } });
    fireEvent.click(screen.getByRole('button', { name: 'Create Profile' }));

    await waitFor(() => expect(onSubmit).toHaveBeenCalledTimes(1));
    expect(onSubmit.mock.calls[0][0].weight).toBeCloseTo(100, 1);
  });

  it('calls cancel without submitting', async () => {
    const user = userEvent.setup();
    const onCancel = vi.fn();
    const onSubmit = vi.fn().mockResolvedValue(undefined);

    render(<ProfileForm onSubmit={onSubmit} onCancel={onCancel} />);

    await user.click(screen.getByRole('button', { name: 'Cancel' }));

    expect(onCancel).toHaveBeenCalledTimes(1);
    expect(onSubmit).not.toHaveBeenCalled();
  });
});
