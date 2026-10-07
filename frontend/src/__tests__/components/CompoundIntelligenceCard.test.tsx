import { CompoundIntelligenceCard } from '@/components/knowledge/CompoundIntelligenceCard';
import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';

vi.mock('@/lib/context', () => ({
  useProfile: () => ({
    currentProfileId: 'profile-1',
    profiles: [
      {
        id: 'profile-1',
        displayName: 'Test User',
        sex: 'Male',
        age: 35,
        weight: 86,
      },
    ],
  }),
}));

vi.mock('@/lib/settings', () => ({
  useSettings: () => ({
    settings: { weightUnit: 'kg' },
  }),
}));

describe('CompoundIntelligenceCard', () => {
  const baseEntry = {
    canonicalName: 'NAD+',
    aliases: [],
    classification: 'Coenzyme',
    regulatoryStatus: 'Supplement',
    mechanismSummary: 'Supports cellular energy pathways.',
    evidenceTier: 'Moderate',
    sourceReferences: [],
    notes: 'Educational use only.',
    pathways: ['cellular-energy', 'mitochondrial-function'],
    benefits: ['Energy support'],
    pairsWellWith: ['MOTS-C'],
    avoidWith: [],
    compatibleBlends: [],
    recommendedDosage: '',
    frequency: '',
    preferredTimeOfDay: '',
    weeklyDosageSchedule: [],
    drugInteractions: [],
    optimizationProtein: '',
    optimizationCarbs: '',
    optimizationSupplements: '',
    optimizationSleep: '',
    optimizationExercise: '',
  };

  it('renders useful observational evidence without contextual product recommendations', () => {
    render(
      <CompoundIntelligenceCard
        entry={baseEntry}
      />
    );

    expect(screen.getByText('Overview')).toBeInTheDocument();
    expect(screen.getByText('Energy support')).toBeInTheDocument();
    expect(screen.queryByText('Common additions')).not.toBeInTheDocument();
    expect(screen.queryByText('MOTS-C')).not.toBeInTheDocument();
  });

  it('withholds dose, schedule, optimization, pairing, and blend fields from the public card', () => {
    const { container } = render(
      <CompoundIntelligenceCard
        entry={{
          ...baseEntry,
          pairsWellWith: ['Pairing candidate'],
          compatibleBlends: ['Co-vial candidate'],
          avoidWith: ['Reported caution'],
          recommendedDosage: '250-500 mg',
          frequency: 'Twice daily',
          preferredTimeOfDay: 'Morning',
          weeklyDosageSchedule: ['Week 1: 250 mg'],
          optimizationProtein: '2 g/kg/day',
          optimizationCarbs: '200 g/day',
          optimizationSupplements: 'Supplement candidate',
          optimizationSleep: '8 hours',
          optimizationExercise: 'Train daily',
        }}
      />
    );

    expect(screen.getByText('Interactions & cautions')).toBeInTheDocument();
    expect(screen.getByText('Flagged in source data')).toBeInTheDocument();
    expect(screen.getByText('Reported caution')).toBeInTheDocument();
    expect(screen.getByText('These are observational flags for review, not individualized instructions.')).toBeInTheDocument();
    expect(container.textContent ?? '').not.toContain('Pairing candidate');
    expect(container.textContent ?? '').not.toContain('Co-vial candidate');
    expect(container.textContent ?? '').not.toContain('250-500 mg');
    expect(container.textContent ?? '').not.toContain('Twice daily');
    expect(container.textContent ?? '').not.toContain('Week 1: 250 mg');
    expect(container.textContent ?? '').not.toContain('2 g/kg/day');
    expect(container.textContent ?? '').not.toContain('Supplement candidate');
    expect(screen.queryByText('Reference Data')).not.toBeInTheDocument();
    expect(screen.queryByText('Optimization Guidelines')).not.toBeInTheDocument();
  });

  it('does not surface MOTS-C-by-age or weight-conditional prescriptive copy', () => {
    // The retired conditional sentences must be unreachable from the JSX,
    // regardless of profile age/weight or canonical compound name.
    const { container } = render(
      <CompoundIntelligenceCard
        entry={{
          ...baseEntry,
          canonicalName: 'MOTS-C',
          recommendedDosage: 'Published range: 5-10 mg/week',
        }}
      />
    );
    expect(container.textContent ?? '').not.toContain('Profile context may warrant closer review');
    expect(container.textContent ?? '').not.toContain('Published MOTS-C context can vary with biological age');
  });

  it('renders benefits as chips when present', () => {
    render(
      <CompoundIntelligenceCard
        entry={baseEntry}
      />
    );

    expect(screen.getByText('Benefits')).toBeInTheDocument();
    expect(screen.getByText('Energy support')).toBeInTheDocument();
  });

  it('renders drug interactions as chips when present', () => {
    render(
      <CompoundIntelligenceCard
        entry={{
          ...baseEntry,
          drugInteractions: ['Warfarin'],
        }}
      />
    );

    expect(screen.getByText('Drug interactions')).toBeInTheDocument();
    expect(screen.getByText('Warfarin')).toBeInTheDocument();
  });

  it('omits benefits and interactions & cautions sections when their arrays are empty', () => {
    render(
      <CompoundIntelligenceCard
        entry={{
          ...baseEntry,
          benefits: [],
          avoidWith: [],
          drugInteractions: [],
        }}
      />
    );

    expect(screen.queryByText('Benefits')).not.toBeInTheDocument();
    expect(screen.queryByText('Interactions & cautions')).not.toBeInTheDocument();
    expect(screen.queryByText('Drug interactions')).not.toBeInTheDocument();
  });

  it('renders only the populated subsection when just one of avoidWith/drugInteractions is present', () => {
    render(
      <CompoundIntelligenceCard
        entry={{
          ...baseEntry,
          avoidWith: [],
          drugInteractions: ['Warfarin'],
        }}
      />
    );

    expect(screen.getByText('Interactions & cautions')).toBeInTheDocument();
    expect(screen.getByText('Drug interactions')).toBeInTheDocument();
    expect(screen.queryByText('Flagged in source data')).not.toBeInTheDocument();
  });

  it('renders short pathway entries as chips and long, sentence-shaped entries as callouts', () => {
    const { container } = render(
      <CompoundIntelligenceCard
        entry={{
          ...baseEntry,
          pathways: [
            'cellular-energy',
            'Ipamorelin is a synthetic pentapeptide ghrelin mimetic that stimulates growth hormone release from the anterior pituitary; it was first identified in 1998.',
          ],
        }}
      />
    );

    const chip = screen.getByText('cellular-energy');
    expect(chip.className).toContain('rounded-full');

    const callout = screen.getByText(/Ipamorelin is a synthetic pentapeptide/);
    expect(callout.tagName).toBe('P');
    expect(callout.className).toContain('rounded-xl');
    expect(callout.className).not.toContain('rounded-full');
    expect(container.querySelectorAll('.rounded-xl').length).toBeGreaterThan(0);
  });
});
