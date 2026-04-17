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

    expect(screen.getByText('What do you want help with first?')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Continue' }));

    const input = await screen.findByPlaceholderText('Type a compound, supplement, or medication…');
    fireEvent.change(input, { target: { value: 'BPC-157' } });
    fireEvent.click(screen.getByRole('button', { name: 'Add' }));
    fireEvent.click(screen.getByRole('button', { name: 'Add to My List' }));

    expect(await screen.findAllByText('Context established.')).not.toHaveLength(0);
    expect(screen.getAllByText('Relationship analysis unavailable.').length).toBeGreaterThan(0);
    expect(screen.queryByText('This is the point.')).not.toBeInTheDocument();
    expect(screen.queryByText('Why this matters')).not.toBeInTheDocument();
    expect(screen.queryByText('Relationship detected.')).not.toBeInTheDocument();
    expect(screen.queryByText('Recovery context: Both were selected by the user, so this is a real input check.')).not.toBeInTheDocument();
    expect(apiClient.checkOverlap).not.toHaveBeenCalled();
  });

  it('gets users from goals to input to earned relationship without an auth wall first', async () => {
    render(<OnboardingExperience />);

    expect(screen.getByText('What do you want help with first?')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Energy' }));
    fireEvent.click(screen.getByRole('button', { name: 'Continue' }));

    expect(
      await screen.findByPlaceholderText('Type a compound, supplement, or medication…')
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

    fireEvent.click(screen.getByRole('button', { name: 'Add to My List' }));

    expect(await screen.findAllByText('Relationship detected.')).not.toHaveLength(0);
    expect(screen.getByText('Recovery context: Both were selected by the user, so this is a real input check.')).toBeInTheDocument();
    expect(screen.getByText('One earned relationship outcome emitted.')).toBeInTheDocument();
    expect(screen.getAllByText('BPC-157').length).toBeGreaterThan(0);
    expect(screen.getAllByText('NAD+').length).toBeGreaterThan(0);

    expect(screen.getByRole('link', { name: 'Finish Setup' })).toHaveAttribute('href', '/profiles');
  });

  it('lets beginners start from an example without knowing compound names', async () => {
    render(<OnboardingExperience />);

    fireEvent.click(screen.getByRole('button', { name: 'Continue' }));
    fireEvent.click(await screen.findByRole('button', { name: /Sleep Reset/ }));

    expect(screen.getByText('Magnesium')).toBeInTheDocument();
    expect(screen.getByText('L-Theanine')).toBeInTheDocument();
    expect(screen.getByText('Glycine')).toBeInTheDocument();
    expect(screen.getByText('3 items added')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('Type a compound, supplement, or medication…')).toBeInTheDocument();
    expect(await screen.findByText('Relationship analysis active.')).toBeInTheDocument();
  });
});
