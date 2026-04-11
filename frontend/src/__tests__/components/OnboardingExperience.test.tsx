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
  },
}));

describe('OnboardingExperience', () => {
  beforeEach(() => {
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
    localStorage.clear();
  });

  it('gets users from input to insight to goals without an auth wall first', async () => {
    render(<OnboardingExperience />);

    expect(
      screen.getByPlaceholderText('Type a compound, supplement, or medication…')
    ).toBeInTheDocument();
    await screen.findByRole('button', { name: 'BPC-157' });

    fireEvent.click(screen.getByRole('button', { name: 'BPC-157' }));
    fireEvent.change(screen.getByPlaceholderText('Type a compound, supplement, or medication…'), {
      target: { value: 'NAD+' },
    });
    fireEvent.keyDown(screen.getByPlaceholderText('Type a compound, supplement, or medication…'), {
      key: 'Enter',
      code: 'Enter',
    });

    fireEvent.click(screen.getByRole('button', { name: 'Add to My Stack' }));

    expect(await screen.findByText('These may be doing the same job')).toBeInTheDocument();
    expect(screen.getByText('You might not need both')).toBeInTheDocument();
    expect(screen.getByText('A lot of people don’t notice this at first')).toBeInTheDocument();
    expect(screen.getAllByText('BPC-157').length).toBeGreaterThan(0);
    expect(screen.getAllByText('NAD+').length).toBeGreaterThan(0);

    fireEvent.click(screen.getByRole('button', { name: 'Continue' }));

    expect(await screen.findByText('What are you trying to improve?')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Energy' }));
    expect(screen.getByRole('link', { name: 'Finish Setup' })).toHaveAttribute('href', '/profiles');
  });
});