import { ProfileSwitcher } from '@/components/ProfileSwitcher';
import { fireEvent, render, screen, cleanup, act } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

const setCurrentProfileId = vi.fn();
const useProfileMock = vi.fn();

vi.mock('@/lib/context', () => ({
  useProfile: () => useProfileMock(),
}));

vi.mock('@/lib/settings', () => ({
  useSettings: () => ({ settings: { weightUnit: 'metric' } }),
}));

const profiles = [
  { id: 'p-1', displayName: 'Alpha', weight: 80 },
  { id: 'p-2', displayName: 'Bravo', weight: 70 },
];

describe('ProfileSwitcher toast', () => {
  beforeEach(() => {
    setCurrentProfileId.mockReset();
    useProfileMock.mockReset();
    useProfileMock.mockReturnValue({
      currentProfileId: 'p-1',
      setCurrentProfileId,
      profiles,
    });
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
    cleanup();
  });

  const openDropdown = () => {
    // Trigger button is the first button in document order before the menu opens.
    fireEvent.click(screen.getAllByRole('button')[0]);
  };

  const clickDropdownOption = (name: RegExp) => {
    // After opening, getAllByRole returns [trigger, ...options]. Match the option whose
    // accessible name includes the target profile name AND a weight unit.
    const buttons = screen.getAllByRole('button', { name });
    const option = buttons.find((button) => /kg|lbs/.test(button.textContent ?? ''));
    if (!option) {
      throw new Error(`Dropdown option not found for ${name}`);
    }
    fireEvent.click(option);
  };

  it('renders "Switched to {Name}" toast after picking a different profile', () => {
    render(<ProfileSwitcher />);

    openDropdown();
    clickDropdownOption(/Bravo/);

    expect(setCurrentProfileId).toHaveBeenCalledWith('p-2');
    const toast = screen.getByTestId('profile-switch-toast');
    expect(toast).toHaveTextContent('Switched to Bravo');
    expect(toast).toHaveAttribute('role', 'status');
    expect(toast).toHaveAttribute('aria-live', 'polite');
  });

  it('does not render the toast when selecting the already-active profile', () => {
    render(<ProfileSwitcher />);

    openDropdown();
    clickDropdownOption(/Alpha/);

    expect(screen.queryByTestId('profile-switch-toast')).not.toBeInTheDocument();
  });

  it('clears the toast after the timeout', () => {
    render(<ProfileSwitcher />);

    openDropdown();
    clickDropdownOption(/Bravo/);

    expect(screen.getByTestId('profile-switch-toast')).toBeInTheDocument();

    act(() => {
      vi.advanceTimersByTime(2700);
    });

    expect(screen.queryByTestId('profile-switch-toast')).not.toBeInTheDocument();
  });
});
