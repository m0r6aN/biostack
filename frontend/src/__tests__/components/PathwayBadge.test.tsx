import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { PathwayBadge, pathwayStyles } from '@/components/ui/PathwayBadge';

describe('PathwayBadge', () => {
  it('renders the label text', () => {
    render(<PathwayBadge label="Tissue repair" />);
    expect(screen.getByText('Tissue repair')).toBeInTheDocument();
  });

  it('applies the correct style for known pathways', () => {
    render(<PathwayBadge label="Inflammation" />);
    const badge = screen.getByText('Inflammation');
    expect(badge.className).toContain('text-amber-300');
  });

  it('applies fallback style for unknown pathways', () => {
    render(<PathwayBadge label="Unknown Pathway" />);
    const badge = screen.getByText('Unknown Pathway');
    expect(badge.className).toContain('bg-white/10');
  });

  it('applies custom className', () => {
    render(<PathwayBadge label="Longevity" className="my-custom" />);
    const badge = screen.getByText('Longevity');
    expect(badge.className).toContain('my-custom');
  });

  it.each([
    'Tissue repair',
    'Inflammation',
    'Recovery',
    'Mobility',
    'Mitochondrial',
    'Metabolism',
    'Exercise signaling',
    'Cellular energy',
    'Longevity',
    'Appetite',
    'Glucose regulation',
  ])('renders known pathway "%s" without error', (label) => {
    expect(() => render(<PathwayBadge label={label} />)).not.toThrow();
  });

  it('exports pathwayStyles with all known pathways', () => {
    expect(pathwayStyles['Tissue repair']).toBeDefined();
    expect(pathwayStyles['Longevity']).toBeDefined();
  });
});

import { StatusChip } from '@/components/ui/StatusChip';

describe('StatusChip', () => {
  it('renders children text', () => {
    render(<StatusChip>Active</StatusChip>);
    expect(screen.getByText('Active')).toBeInTheDocument();
  });

  it('renders icon when provided', () => {
    render(<StatusChip icon={<span data-testid="chip-icon">★</span>}>Status</StatusChip>);
    expect(screen.getByTestId('chip-icon')).toBeInTheDocument();
  });

  it('does not render icon wrapper when icon is not provided', () => {
    render(<StatusChip>No icon</StatusChip>);
    // No emerald-400 span (icon wrapper) rendered
    const el = screen.getByText('No icon').parentElement;
    expect(el?.querySelector('.text-emerald-400')).toBeNull();
  });

  it('applies custom className', () => {
    render(<StatusChip className="my-chip">Chip</StatusChip>);
    const chip = screen.getByText('Chip').closest('div');
    expect(chip?.className).toContain('my-chip');
  });

  it('renders without crashing when given various children', () => {
    expect(() => render(<StatusChip>0 compounds</StatusChip>)).not.toThrow();
    expect(() => render(<StatusChip><strong>Bold</strong></StatusChip>)).not.toThrow();
  });
});
