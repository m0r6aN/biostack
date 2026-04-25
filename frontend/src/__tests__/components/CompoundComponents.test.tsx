import { CompoundForm } from '@/components/compounds/CompoundForm';
import { CompoundList } from '@/components/compounds/CompoundList';
import { apiClient } from '@/lib/api';
import type { CompoundRecord, KnowledgeEntry } from '@/lib/types';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('@/lib/api', () => ({
  apiClient: {
    getAllKnowledgeCompounds: vi.fn(),
  },
}));

const knowledgeEntries: KnowledgeEntry[] = [
  {
    canonicalName: 'BPC-157',
    aliases: [],
    classification: 'Peptide',
    regulatoryStatus: 'Research',
    mechanismSummary: 'Tissue support',
    evidenceTier: 'Moderate',
    sourceReferences: [],
    notes: '',
    pathways: [],
    benefits: ['recovery', 'joint comfort'],
    pairsWellWith: [],
    avoidWith: [],
    compatibleBlends: [],
    vialCompatibility: '',
    recommendedDosage: '',
    standardDosageRange: '',
    maxReportedDose: '',
    frequency: '',
    preferredTimeOfDay: '',
    weeklyDosageSchedule: [],
    incrementalEscalationSteps: [],
    drugInteractions: [],
    optimizationProtein: '',
    optimizationCarbs: '',
    optimizationSupplements: [],
    optimizationSleep: '',
    optimizationExercise: '',
  },
  {
    canonicalName: 'Creatine',
    aliases: [],
    classification: 'Supplement',
    regulatoryStatus: 'Supplement',
    mechanismSummary: 'Performance support',
    evidenceTier: 'Strong',
    sourceReferences: [],
    notes: '',
    pathways: [],
    benefits: ['strength'],
    pairsWellWith: [],
    avoidWith: [],
    compatibleBlends: [],
    vialCompatibility: '',
    recommendedDosage: '',
    standardDosageRange: '',
    maxReportedDose: '',
    frequency: '',
    preferredTimeOfDay: '',
    weeklyDosageSchedule: [],
    incrementalEscalationSteps: [],
    drugInteractions: [],
    optimizationProtein: '',
    optimizationCarbs: '',
    optimizationSupplements: [],
    optimizationSleep: '',
    optimizationExercise: '',
  },
];

const compounds: CompoundRecord[] = [
  {
    id: 'compound-1',
    personId: 'person-1',
    name: 'BPC-157',
    category: 'Peptide',
    startDate: '2026-01-01T00:00:00Z',
    endDate: null,
    status: 'Active',
    notes: 'Morning dose',
    sourceType: 'Manual',
    goal: 'recovery',
  },
];

describe('CompoundForm', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(apiClient.getAllKnowledgeCompounds).mockResolvedValue(knowledgeEntries);
  });

  it('filters goals and compounds by category before submitting a compound', async () => {
    const user = userEvent.setup();
    const onSubmit = vi.fn().mockResolvedValue(undefined);

    render(<CompoundForm personId="person-1" onSubmit={onSubmit} />);

    await waitFor(() => expect(apiClient.getAllKnowledgeCompounds).toHaveBeenCalledTimes(1));
    fireEvent.change(screen.getByLabelText('1. Select a Category'), { target: { value: 'Peptide' } });

    expect(screen.getByRole('option', { name: 'Recovery' })).toBeInTheDocument();
    expect(screen.queryByRole('option', { name: 'Strength' })).not.toBeInTheDocument();

    fireEvent.change(screen.getByLabelText('2. Select a Goal'), { target: { value: 'recovery' } });
    fireEvent.change(screen.getByLabelText('3. Select a Compound'), { target: { value: 'BPC-157' } });
    await user.type(screen.getByLabelText('5a. Optional: Source'), 'Clinic');
    await user.type(screen.getByLabelText('5b. Optional: Price Paid'), '120.50');
    await user.type(screen.getByLabelText('Notes'), 'Track recovery response.');
    await user.click(screen.getByRole('button', { name: 'Add Compound' }));

    await waitFor(() => expect(onSubmit).toHaveBeenCalledTimes(1));
    expect(onSubmit).toHaveBeenCalledWith(expect.objectContaining({
      personId: 'person-1',
      name: 'BPC-157',
      category: 'Peptide',
      goal: 'recovery',
      source: 'Clinic',
      pricePaid: 120.5,
      status: 'Active',
      notes: 'Track recovery response.',
      endDate: null,
    }));
  });

  it('falls back to manual compound entry when the list does not contain a match', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined);

    render(<CompoundForm personId="person-1" onSubmit={onSubmit} />);

    await waitFor(() => expect(apiClient.getAllKnowledgeCompounds).toHaveBeenCalledTimes(1));
    fireEvent.change(screen.getByLabelText('1. Select a Category'), { target: { value: 'Other' } });
    fireEvent.change(screen.getByLabelText('4. Optional: Manual Search/Entry'), { target: { value: 'Custom Blend' } });
    fireEvent.click(screen.getByRole('button', { name: 'Add Compound' }));

    await waitFor(() => expect(onSubmit).toHaveBeenCalledTimes(1));
    expect(onSubmit).toHaveBeenCalledWith(expect.objectContaining({
      name: 'Custom Blend',
      category: 'Other',
      personId: 'person-1',
    }));
  });
});

describe('CompoundList', () => {
  it('renders compound metadata and sends the selected compound to the caller', async () => {
    const user = userEvent.setup();
    const onSelect = vi.fn();

    render(<CompoundList compounds={compounds} onSelect={onSelect} />);

    expect(screen.getByText('BPC-157')).toBeInTheDocument();
    expect(screen.getByText('Peptide')).toBeInTheDocument();
    expect(screen.getByText('recovery')).toBeInTheDocument();
    expect(screen.getByText('Morning dose')).toBeInTheDocument();

    await user.click(screen.getByText('BPC-157'));

    expect(onSelect).toHaveBeenCalledWith(compounds[0]);
  });
});
