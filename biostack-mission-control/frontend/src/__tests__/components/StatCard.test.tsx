import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { StatCard } from '@/components/dashboard/StatCard';

describe('StatCard', () => {
  it('renders the title', () => {
    render(<StatCard title="Active Compounds" value={3} icon="🧪" />);
    expect(screen.getByText('Active Compounds')).toBeInTheDocument();
  });

  it('renders the numeric value', () => {
    render(<StatCard title="Total" value={42} icon="📊" />);
    expect(screen.getByText('42')).toBeInTheDocument();
  });

  it('renders a string value (e.g. formatted weight)', () => {
    render(<StatCard title="Weight" value="75 kg" icon="⚖️" />);
    expect(screen.getByText('75 kg')).toBeInTheDocument();
  });

  it('renders the unit when provided', () => {
    render(<StatCard title="Weight" value={75} unit="kg" icon="⚖️" />);
    expect(screen.getByText('kg')).toBeInTheDocument();
  });

  it('renders the icon', () => {
    render(<StatCard title="Compounds" value={5} icon="🧪" />);
    expect(screen.getByText('🧪')).toBeInTheDocument();
  });

  it('renders without crashing with all color variants', () => {
    const colors = ['emerald', 'blue', 'amber', 'red', 'default'] as const;
    for (const color of colors) {
      expect(() =>
        render(<StatCard title="X" value={1} icon="·" color={color} />)
      ).not.toThrow();
    }
  });
});
