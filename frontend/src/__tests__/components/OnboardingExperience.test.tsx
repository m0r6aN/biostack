import { OnboardingExperience } from '@/components/marketing/OnboardingExperience';
import { apiClient } from '@/lib/api';
import { fireEvent, render, screen } from '@testing-library/react';
import type { ComponentProps } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('next/link', () => ({
  default: ({ href, children, ...props }: ComponentProps<'a'>) => (
    <a href={href} {...props}>
      {children}
    </a>
  ),
}));

vi.mock('@/lib/api', () => ({
  apiClient: {
    getAllKnowledgeCompounds: vi.fn(),
    checkOverlap: vi.fn(),
  },
}));

describe('OnboardingExperience', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(apiClient.getAllKnowledgeCompounds).mockResolvedValue([
      {
        canonicalName: 'BPC-157',
        aliases: [],
        classification: 'Peptide',
        regulatoryStatus: '',
        mechanismSummary: '',
        evidenceTier: '',
        sourceReferences: [],
        notes: '',
        pathways: [],
        benefits: [],
        pairsWellWith: [],
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
      },
      {
        canonicalName: 'NAD+',
        aliases: [],
        classification: 'Supplement',
        regulatoryStatus: '',
        mechanismSummary: '',
        evidenceTier: '',
        sourceReferences: [],
        notes: '',
        pathways: [],
        benefits: [],
        pairsWellWith: [],
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
      },
    ]);
    vi.mocked(apiClient.checkOverlap).mockResolvedValue([
      {
        compoundNames: ['BPC-157', 'NAD+'],
        overlapType: 'SharedPathway',
        pathwayTag: 'Recovery context',
        description: 'Both were selected by the user, so this is a real input check.',
        evidenceConfidence: 'Limited',
      },
    ]);
    localStorage.clear();
  });

  it('shows context only for one input and keeps relationship analysis locked', async () => {
    render(<OnboardingExperience />);

    const input = screen.getByPlaceholderText('Type a compound, supplement, or medication…');
    fireEvent.change(input, { target: { value: 'BPC-157' } });
    fireEvent.click(screen.getByRole('button', { name: 'Add' }));
    fireEvent.click(screen.getByRole('button', { name: 'Add to My Protocol' }));

    expect(await screen.findByText('Context is established. Relationship analysis remains locked.')).toBeInTheDocument();
    expect(screen.getByText('Relationship analysis unavailable - requires additional inputs.')).toBeInTheDocument();
    expect(screen.queryByText('Relationship detected.')).not.toBeInTheDocument();
    expect(screen.queryByText('Recovery context: Both were selected by the user, so this is a real input check.')).not.toBeInTheDocument();
    expect(apiClient.checkOverlap).not.toHaveBeenCalled();
  });

  it('gets users from input to earned relationship to goals without an auth wall first', async () => {
    render(<OnboardingExperience />);

    expect(
      screen.getByPlaceholderText('Type a compound, supplement, or medication…')
    ).toBeInTheDocument();
    fireEvent.change(screen.getByPlaceholderText('Type a compound, supplement, or medication…'), {
      target: { value: 'BPC-157' },
    });
    fireEvent.click(screen.getByRole('button', { name: 'Add' }));
    fireEvent.change(screen.getByPlaceholderText('Type a compound, supplement, or medication…'), {
      target: { value: 'NAD+' },
    });
    fireEvent.keyDown(screen.getByPlaceholderText('Type a compound, supplement, or medication…'), {
      key: 'Enter',
      code: 'Enter',
    });

    fireEvent.click(screen.getByRole('button', { name: 'Add to My Protocol' }));

    expect(await screen.findByText('Relationship detected.')).toBeInTheDocument();
    expect(screen.getByText('Recovery context: Both were selected by the user, so this is a real input check.')).toBeInTheDocument();
    expect(screen.getByText('Only one earned relationship outcome is shown.')).toBeInTheDocument();
    expect(screen.getAllByText('BPC-157').length).toBeGreaterThan(0);
    expect(screen.getAllByText('NAD+').length).toBeGreaterThan(0);

    fireEvent.click(screen.getByRole('button', { name: 'Continue' }));

    expect(await screen.findByText('What should BioStack watch first?')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Energy' }));
    expect(screen.getByRole('link', { name: 'Finish Setup' })).toHaveAttribute('href', '/profiles');
  });
});
