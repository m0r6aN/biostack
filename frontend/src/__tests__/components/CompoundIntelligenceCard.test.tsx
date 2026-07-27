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

    expect(screen.getByText('Mechanism Summary')).toBeInTheDocument();
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

    expect(screen.getByText('Reported cautions in source data')).toBeInTheDocument();
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

    expect(screen.getByText('Drug Interactions')).toBeInTheDocument();
    expect(screen.getByText('Warfarin')).toBeInTheDocument();
  });

  it('omits benefits and drug interactions sections when their arrays are empty', () => {
    render(
      <CompoundIntelligenceCard
        entry={{
          ...baseEntry,
          benefits: [],
          drugInteractions: [],
        }}
      />
    );

    expect(screen.queryByText('Benefits')).not.toBeInTheDocument();
    expect(screen.queryByText('Drug Interactions')).not.toBeInTheDocument();
  });
});
