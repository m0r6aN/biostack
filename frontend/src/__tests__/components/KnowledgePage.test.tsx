import KnowledgePage from '@/app/knowledge/page';
import { apiClient } from '@/lib/api';
import { fireEvent, render, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('@/components/Header', () => ({
  Header: ({ title }: { title: string }) => <header>{title}</header>,
}));

vi.mock('@/components/knowledge/CompoundIntelligenceCard', () => ({
  CompoundIntelligenceCard: ({ entry }: { entry: { canonicalName: string } }) => (
    <div>{entry.canonicalName}</div>
  ),
}));

vi.mock('@/lib/api', () => ({
  apiClient: {
    getAllKnowledgeCompounds: vi.fn(),
    checkOverlap: vi.fn(),
  },
}));

const knowledgeEntry = {
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
};

describe('KnowledgePage overlap gating', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(apiClient.getAllKnowledgeCompounds).mockResolvedValue([knowledgeEntry]);
    vi.mocked(apiClient.checkOverlap).mockResolvedValue([]);
  });

  it('does not render overlap results with one selected compound', async () => {
    render(<KnowledgePage />);

    fireEvent.change(screen.getByPlaceholderText('Search compounds, supplements, substances…'), {
      target: { value: 'BPC' },
    });
    fireEvent.click(screen.getByRole('button', { name: 'Search' }));

    const selectionButton = await screen.findByRole('button', { name: 'BPC-157' });
    fireEvent.click(selectionButton);

    expect(
      screen.getByText((_, element) => element?.textContent === '1 compound selected')
    ).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Check Overlaps' })).toBeDisabled();
    expect(screen.queryByText('No pathway overlaps detected for selected compounds.')).not.toBeInTheDocument();
    expect(apiClient.checkOverlap).not.toHaveBeenCalled();
  });
});
