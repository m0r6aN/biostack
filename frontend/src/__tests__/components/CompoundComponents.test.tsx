import { CompoundForm } from '@/components/compounds/CompoundForm';
import { CompoundList } from '@/components/compounds/CompoundList';
import { apiClient } from '@/lib/api';
import type { CompoundRecord, KnowledgeEntry } from '@/lib/types';
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
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

  it('uses concise research labels while preserving the original goal for filtering and saving', async () => {
    const original = 'Evidence reviewed for hypoactive sexual desire disorder per product label; BioStack presents this as educational reference, not a recommendation.';
    const generic = 'Evidence reviewed for research reference; BioStack presents this as educational reference, not a recommendation.';
    vi.mocked(apiClient.getAllKnowledgeCompounds).mockResolvedValue([
      { ...knowledgeEntries[0], canonicalName: 'Bremelanotide', benefits: [original] },
      { ...knowledgeEntries[0], canonicalName: 'Research compound', benefits: [generic] },
      knowledgeEntries[0],
    ]);
    const onSubmit = vi.fn().mockResolvedValue(undefined);
    render(<CompoundForm personId="person-1" onSubmit={onSubmit} />);
    await waitFor(() => expect(apiClient.getAllKnowledgeCompounds).toHaveBeenCalled());
    fireEvent.change(screen.getByLabelText('1. Select a Category'), { target: { value: 'Peptide' } });
    expect(screen.getByRole('option', { name: 'Hypoactive sexual desire disorder' })).toHaveValue(original);
    expect(screen.getByRole('option', { name: 'Research reference' })).toHaveValue(generic);
    expect(screen.queryByRole('option', { name: original })).not.toBeInTheDocument();
    expect(screen.getByRole('option', { name: 'Recovery' })).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText('2. Select a Goal'), { target: { value: original } });
    expect(screen.getByLabelText('2. Select a Goal')).toHaveAccessibleDescription(original);
    // Default view is the whole category: the goal match (Bremelanotide) sorts
    // first and is marked, but non-matching compounds stay visible too.
    expect(screen.getByRole('option', { name: 'Bremelanotide — matches your goal' })).toBeInTheDocument();
    expect(screen.getByRole('option', { name: 'Research compound' })).toBeInTheDocument();
    const compoundOptionsForOriginalGoal = within(screen.getByLabelText('3. Select a Compound')).getAllByRole('option');
    expect(compoundOptionsForOriginalGoal.map(o => o.textContent)).toEqual([
      'Select a compound...',
      'Bremelanotide — matches your goal',
      'BPC-157',
      'Research compound',
    ]);
    fireEvent.change(screen.getByLabelText('3. Select a Compound'), { target: { value: 'Bremelanotide' } });
    fireEvent.click(screen.getByRole('button', { name: 'Add Compound' }));
    await waitFor(() => expect(onSubmit).toHaveBeenCalledWith(expect.objectContaining({ goal: original, name: 'Bremelanotide' })));
  });

  // Regression note (design decision, owner-feedback-20260915 #5): PR #352 shipped a
  // "goal matches only" default with a toggle to reveal the rest of the category. The
  // owner found that unfriendly and lost a compound (MOTS-C) hiding behind it. The
  // binding lead decision replaces the hide/reveal toggle with an always-full category
  // list, sorted goal-matches-first with a "matches your goal" marker — so a sparse
  // goal (matching only one compound) never hides the rest of the category, and there
  // is nothing left to toggle. This test replaces the old
  // "lets a sparse goal browse the category and save another compound with that goal".
  it('shows the whole category by default and sorts a sparse goal match first', async () => {
    vi.mocked(apiClient.getAllKnowledgeCompounds).mockResolvedValue([
      { ...knowledgeEntries[0], canonicalName: 'MOTS-C', benefits: ['anti-aging'] },
      knowledgeEntries[0],
      knowledgeEntries[1],
    ]);
    const onSubmit = vi.fn().mockResolvedValue(undefined);
    render(<CompoundForm personId="person-1" onSubmit={onSubmit} />);
    await waitFor(() => expect(apiClient.getAllKnowledgeCompounds).toHaveBeenCalled());
    fireEvent.change(screen.getByLabelText('1. Select a Category'), { target: { value: 'Peptide' } });

    // No goal yet: the whole category is visible, alphabetically, no other-category
    // compound (Creatine) leaks in, and there is no goal-match messaging.
    expect(screen.getByRole('option', { name: 'BPC-157' })).toBeInTheDocument();
    expect(screen.getByRole('option', { name: 'MOTS-C' })).toBeInTheDocument();
    expect(screen.queryByRole('option', { name: 'Creatine' })).not.toBeInTheDocument();
    expect(screen.queryByRole('status')).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /show all compounds/i })).not.toBeInTheDocument();

    fireEvent.change(screen.getByLabelText('2. Select a Goal'), { target: { value: 'anti-aging' } });

    // A sparse goal match (only MOTS-C) still leaves the rest of the category visible;
    // the match is marked and sorted first instead of hiding BPC-157.
    const compoundOptions = within(screen.getByLabelText('3. Select a Compound')).getAllByRole('option');
    expect(compoundOptions.map(o => o.textContent)).toEqual([
      'Select a compound...',
      'MOTS-C — matches your goal',
      'BPC-157',
    ]);
    expect(screen.getByRole('status')).toHaveTextContent('Compounds that match your goal are shown first in this category.');
    expect(screen.getByRole('status')).toHaveTextContent('Goal tags are not a complete list of compounds or evidence of effectiveness.');

    fireEvent.change(screen.getByLabelText('3. Select a Compound'), { target: { value: 'BPC-157' } });
    fireEvent.click(screen.getByRole('button', { name: 'Add Compound' }));
    await waitFor(() => expect(onSubmit).toHaveBeenCalledWith(expect.objectContaining({ name: 'BPC-157', goal: 'anti-aging', category: 'Peptide' })));

    // Re-entering the category with a different goal ("recovery", which is BPC-157's
    // benefit, not MOTS-C's) still shows both compounds — MOTS-C simply loses its
    // marker instead of disappearing, and BPC-157 gains one.
    fireEvent.change(screen.getByLabelText('1. Select a Category'), { target: { value: 'Peptide' } });
    fireEvent.change(screen.getByLabelText('2. Select a Goal'), { target: { value: 'recovery' } });
    expect(screen.getByRole('option', { name: 'MOTS-C' })).toBeInTheDocument();
    expect(screen.getByRole('option', { name: 'BPC-157 — matches your goal' })).toBeInTheDocument();
  });

  it('submits calendar dates as explicit UTC timestamps without changing the selected day', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined);
    render(<CompoundForm personId="person-1" onSubmit={onSubmit} />);
    await waitFor(() => expect(apiClient.getAllKnowledgeCompounds).toHaveBeenCalled());
    fireEvent.change(screen.getByLabelText('1. Select a Category'), { target: { value: 'Peptide' } });
    fireEvent.change(screen.getByLabelText('4. Optional: Manual Search/Entry'), { target: { value: 'Fixture' } });
    fireEvent.change(screen.getByLabelText('Start Date'), { target: { value: '2026-09-14' } });
    fireEvent.change(screen.getByLabelText('End Date (Optional)'), { target: { value: '2026-09-20' } });
    fireEvent.click(screen.getByRole('button', { name: 'Add Compound' }));
    await waitFor(() => expect(onSubmit).toHaveBeenCalledWith(expect.objectContaining({
      startDate: '2026-09-14T00:00:00.000Z', endDate: '2026-09-20T00:00:00.000Z', personId: 'person-1',
    })));
  });
});

describe('CompoundList', () => {
  it('keeps historical long goal values readable without rewriting the saved record', () => {
    const goal = 'Evidence reviewed for research reference; BioStack presents this as educational reference, not a recommendation.';
    const saved = { ...compounds[0], goal };
    const onSelect = vi.fn();
    render(<CompoundList compounds={[saved]} onSelect={onSelect} />);
    expect(screen.getByText('Research reference')).toHaveAttribute('title', goal);
    fireEvent.click(screen.getByText('Research reference'));
    expect(onSelect).toHaveBeenCalledWith(saved);
  });
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
