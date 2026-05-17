import { ActiveProfileChip } from '@/components/ActiveProfileChip';
import { render, screen, cleanup } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

const useProfileMock = vi.fn();

vi.mock('@/lib/context', () => ({
  useProfile: () => useProfileMock(),
}));

describe('ActiveProfileChip', () => {
  beforeEach(() => {
    useProfileMock.mockReset();
  });

  afterEach(() => {
    cleanup();
  });

  it('renders the active profile display name when a profile is selected', () => {
    useProfileMock.mockReturnValue({
      currentProfileId: 'p-1',
      profiles: [
        { id: 'p-1', displayName: 'Riley Vega', weight: 80 },
        { id: 'p-2', displayName: 'Other', weight: 70 },
      ],
    });

    render(<ActiveProfileChip />);

    const chip = screen.getByTestId('active-profile-chip');
    expect(chip).toHaveTextContent('Riley Vega');
    expect(chip).toHaveAccessibleName('Active profile: Riley Vega');
    expect(chip).toHaveTextContent(/viewing/i);
  });

  it('updates when the active profile changes', () => {
    useProfileMock.mockReturnValue({
      currentProfileId: 'p-1',
      profiles: [
        { id: 'p-1', displayName: 'Alpha', weight: 80 },
        { id: 'p-2', displayName: 'Bravo', weight: 70 },
      ],
    });

    const { rerender } = render(<ActiveProfileChip />);
    expect(screen.getByTestId('active-profile-chip')).toHaveTextContent('Alpha');

    useProfileMock.mockReturnValue({
      currentProfileId: 'p-2',
      profiles: [
        { id: 'p-1', displayName: 'Alpha', weight: 80 },
        { id: 'p-2', displayName: 'Bravo', weight: 70 },
      ],
    });

    rerender(<ActiveProfileChip />);
    expect(screen.getByTestId('active-profile-chip')).toHaveTextContent('Bravo');
  });

  it('renders nothing when no profile is selected', () => {
    useProfileMock.mockReturnValue({
      currentProfileId: null,
      profiles: [],
    });

    const { container } = render(<ActiveProfileChip />);
    expect(container).toBeEmptyDOMElement();
  });
});
