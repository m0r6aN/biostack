import { CommunitySignalBadge } from '@/components/research/CommunitySignalBadge';
import type { CommunitySignal } from '@/lib/research/types';
import { render, screen } from '@testing-library/react';

function signal(overrides: Partial<CommunitySignal> = {}): CommunitySignal {
  return {
    present: true,
    signalStrength: 'recurring',
    signalDirection: 'positive',
    signalUse: 'stack-pattern',
    canonicalTruthStatus: 'unsupported',
    notes: null,
    ...overrides,
  };
}

describe('CommunitySignalBadge', () => {
  it('renders a badge labeled with the title-cased strength', () => {
    render(<CommunitySignalBadge signal={signal({ signalStrength: 'recurring' })} />);
    expect(screen.getByText(/Community signal/i)).toBeInTheDocument();
    expect(screen.getByText(/Recurring/i)).toBeInTheDocument();
  });

  it('renders an "Isolated" label for isolated signals', () => {
    render(<CommunitySignalBadge signal={signal({ signalStrength: 'isolated' })} />);
    expect(screen.getByText(/Isolated/i)).toBeInTheDocument();
  });

  it('renders a "Widespread" label for widespread signals', () => {
    render(<CommunitySignalBadge signal={signal({ signalStrength: 'widespread' })} />);
    expect(screen.getByText(/Widespread/i)).toBeInTheDocument();
  });

  it('renders a "None" label for absent signals', () => {
    render(<CommunitySignalBadge signal={signal({ signalStrength: 'none', present: false })} />);
    expect(screen.getByText(/None/i)).toBeInTheDocument();
  });

  it('applies strength-specific tone classes', () => {
    const { container, rerender } = render(
      <CommunitySignalBadge signal={signal({ signalStrength: 'recurring' })} />
    );
    const badge = container.querySelector('span');
    expect(badge?.className).toMatch(/violet/);

    rerender(<CommunitySignalBadge signal={signal({ signalStrength: 'widespread' })} />);
    expect(container.querySelector('span')?.className).toMatch(/fuchsia/);

    rerender(<CommunitySignalBadge signal={signal({ signalStrength: 'isolated' })} />);
    expect(container.querySelector('span')?.className).toMatch(/blue/);
  });

  it('exposes canonical truth status via the title attribute', () => {
    const { container } = render(
      <CommunitySignalBadge signal={signal({ canonicalTruthStatus: 'contradicted' })} />
    );
    const badge = container.querySelector('span');
    expect(badge?.getAttribute('title')).toMatch(/Canonical truth/i);
    expect(badge?.getAttribute('title')).toMatch(/Contradicted/i);
  });
});
