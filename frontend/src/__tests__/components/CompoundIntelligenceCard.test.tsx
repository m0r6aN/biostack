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

  it('renders contextual recommendations quietly inside the compound detail surface when relevant', () => {
    render(
      <CompoundIntelligenceCard
        entry={baseEntry}
      />
    );

    expect(screen.getByText('Mechanism Summary')).toBeInTheDocument();
    expect(screen.getByText('Common additions')).toBeInTheDocument();
    expect(screen.getByText('CoQ10')).toBeInTheDocument();
    expect(
      screen.getByText('Here are a few common examples people look at in similar mitochondrial-support contexts.')
    ).toBeInTheDocument();
    expect(screen.getAllByText(/Example source · /).length).toBeGreaterThan(0);
    expect(screen.getByText('Some links may be affiliate links.')).toBeInTheDocument();
  });

  it('uses educational copy variants on knowledge-search surfaces', () => {
    render(
      <CompoundIntelligenceCard
        recommendationSurface="knowledge-search"
        entry={baseEntry}
      />
    );

    expect(
      screen.getByText('People exploring mitochondrial-support compounds often look at these examples next.')
    ).toBeInTheDocument();
  });

  it('uses reference-oriented profile copy instead of prescriptive guidance', () => {
    render(
      <CompoundIntelligenceCard
        entry={{
          ...baseEntry,
          recommendedDosage: 'Published range: 250-500 mg',
          frequency: 'Published schedule varies',
        }}
      />
    );

    expect(screen.getByText('Profile Context')).toBeInTheDocument();
    expect(screen.getByText('Published Range Context')).toBeInTheDocument();
    expect(screen.getByText('General published range referenced.')).toBeInTheDocument();
    expect(screen.getByText('Reference only. Published ranges are not BioStack recommendations.')).toBeInTheDocument();
    expect(screen.getByText('Reference Data')).toBeInTheDocument();
    expect(screen.getByText('Published Range')).toBeInTheDocument();
    expect(screen.getByText('Published ranges are reference data only and are not dosing instructions.')).toBeInTheDocument();
    expect(screen.queryByText('Personalized Guidance')).not.toBeInTheDocument();
    expect(screen.queryByText('Personalized Adjustments')).not.toBeInTheDocument();
    expect(screen.queryByText('Higher end of dosage range recommended.')).not.toBeInTheDocument();
    expect(screen.queryByText('Standard dosage range applicable.')).not.toBeInTheDocument();
    expect(screen.queryByText('Protocol Guidance')).not.toBeInTheDocument();
  });
});
