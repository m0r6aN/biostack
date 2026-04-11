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
  it('renders contextual recommendations quietly inside the compound detail surface when relevant', () => {
    render(
      <CompoundIntelligenceCard
        entry={{
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
        }}
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
        entry={{
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
        }}
      />
    );

    expect(
      screen.getByText('People exploring mitochondrial-support compounds often look at these examples next.')
    ).toBeInTheDocument();
  });
});