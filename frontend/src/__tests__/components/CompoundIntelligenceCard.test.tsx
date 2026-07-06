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
    const { container } = render(
      <CompoundIntelligenceCard
        entry={{
          ...baseEntry,
          recommendedDosage: 'Published range: 250-500 mg',
          frequency: 'Published schedule varies',
        }}
      />
    );

    // Profile Context section renders with demographics only — no adjacent dosage strings.
    const profileContextHeader = screen.getByText('Profile Context');
    expect(profileContextHeader).toBeInTheDocument();

    // Reference Data section renders the published range under the new literature label
    // with the "Reference only" disclaimer above it.
    expect(screen.getByText('Reference Data')).toBeInTheDocument();
    expect(screen.getByText('Published reference range (literature)')).toBeInTheDocument();
    expect(screen.getByText('Reference only. Published ranges are not BioStack recommendations.')).toBeInTheDocument();

    // Disclaimer must appear before the range datum in DOM order so it
    // reads as the qualifier, not the footer.
    const disclaimer = screen.getByText('Reference only. Published ranges are not BioStack recommendations.');
    const rangeValue = screen.getByText('Published range: 250-500 mg');
    expect(disclaimer.compareDocumentPosition(rangeValue) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();

    // The Profile Context section must not contain the published range or
    // any profile-conditional dosage messaging (decoupling guarantee).
    const profileSection = profileContextHeader.closest('div')?.parentElement;
    expect(profileSection).not.toBeNull();
    expect(profileSection!.textContent ?? '').not.toContain('Published range: 250-500 mg');
    expect(profileSection!.textContent ?? '').not.toContain('Published Range Context');

    // Retired prescriptive/conditional strings must not render anywhere.
    expect(screen.queryByText('Published Range Context')).not.toBeInTheDocument();
    expect(screen.queryByText('General published range referenced.')).not.toBeInTheDocument();
    expect(screen.queryByText('Profile context may warrant closer review of published ranges.')).not.toBeInTheDocument();
    expect(screen.queryByText(/Published ranges are reference data only and are not dosing instructions/)).not.toBeInTheDocument();
    expect(screen.queryByText('Personalized Guidance')).not.toBeInTheDocument();
    expect(screen.queryByText('Personalized Adjustments')).not.toBeInTheDocument();
    expect(screen.queryByText('Higher end of dosage range recommended.')).not.toBeInTheDocument();
    expect(screen.queryByText('Standard dosage range applicable.')).not.toBeInTheDocument();
    expect(screen.queryByText('Protocol Guidance')).not.toBeInTheDocument();

    // Defensive: rendered output for this card should not contain the
    // weight/age conditional prescriptive sentences anywhere in the DOM.
    expect(container.textContent ?? '').not.toContain('may warrant closer review');
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
