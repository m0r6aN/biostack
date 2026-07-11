import { SyringeDrawVisualizer } from '@/components/calculators/SyringeDrawVisualizer';
import { VialVisualizer } from '@/components/calculators/VialVisualizer';
import { calculateUnifiedDosing, type UnifiedDosingResult } from '@/lib/dosingCalculator';
import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';

function resultForUnits(units: number) {
  return calculateUnifiedDosing({
    powderAmount: 10,
    powderUnit: 'mg',
    diluentVolumeMl: 10,
    concentrationSource: 'reconstitution',
    knownConcentration: 1000,
    concentrationUnit: 'mcg/mL',
    desiredDose: units * 10,
    desiredDoseUnit: 'mcg',
    doseBasis: 'per-dose',
    splitCount: 1,
  });
}

describe('SyringeDrawVisualizer', () => {
  it.each([
    [12.5, '12.5 U-100 units, equal to 0.125 milliliters'],
    [10, '10 U-100 units, equal to 0.1 milliliters'],
    [50, '50 U-100 units, equal to 0.5 milliliters'],
    [100, '100 U-100 units, equal to 1 milliliters'],
  ])('shows a clear accessible draw for %s units', (units, text) => {
    const { container } = render(<SyringeDrawVisualizer result={resultForUnits(units)} />);
    const meter = screen.getByRole('meter', { name: /calculated draw/i });

    expect(meter).toHaveAttribute('aria-valuenow', String(units));
    expect(meter).toHaveAttribute('aria-valuetext', text);
    expect(screen.getByTestId('draw-marker')).toBeInTheDocument();
    expect(container.querySelectorAll('[data-tick="major"]')).toHaveLength(11);
    expect(container.querySelectorAll('[data-tick="minor"]')).toHaveLength(10);
  });

  it('shows no meter or misleading fill for zero or invalid input', () => {
    const { rerender } = render(<SyringeDrawVisualizer result={null} error="Desired dose must be greater than 0" />);
    expect(screen.queryByRole('meter')).not.toBeInTheDocument();
    expect(screen.getByRole('status')).toHaveTextContent('No draw is shown');

    rerender(<SyringeDrawVisualizer result={{ ...resultForUnits(10), u100UnitsPerAdministration: Number.NaN } as UnifiedDosingResult} />);
    expect(screen.queryByRole('meter')).not.toBeInTheDocument();
  });

  it('caps the visual while announcing the full over-capacity result', () => {
    render(<SyringeDrawVisualizer result={resultForUnits(150)} />);
    expect(screen.getByRole('alert')).toHaveTextContent('above the 100-unit capacity');
    expect(screen.getByRole('meter')).toHaveAttribute('aria-valuenow', '100');
    expect(screen.getByRole('meter').getAttribute('aria-valuetext')).toContain('150 U-100 units');
  });
});

describe('VialVisualizer', () => {
  it('updates its text equivalent and distinguishes powder from liquid', () => {
    render(<VialVisualizer powderAmount={7.5} powderUnit="mg" diluentVolumeMl={2.5} mode="mix" concentrationSource="reconstitution" />);
    expect(screen.getByRole('img')).toHaveAccessibleName('Vial showing 7.5 mg of dry powder and 2.5 milliliters of added liquid');
    expect(screen.getByTestId('vial-powder')).toBeInTheDocument();
    expect(screen.getByTestId('vial-liquid')).toBeInTheDocument();
    expect(screen.getByText('Educational representation only; it does not depict preparation or administration technique.')).toBeInTheDocument();
  });

  it('does not depict liquid for known-concentration mode', () => {
    render(<VialVisualizer powderAmount={5} powderUnit="mg" diluentVolumeMl={2} mode="dose" concentrationSource="known" />);
    expect(screen.queryByTestId('vial-liquid')).not.toBeInTheDocument();
    expect(screen.getByText('Not used')).toBeInTheDocument();
  });
});
