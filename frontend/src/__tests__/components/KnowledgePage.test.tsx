import KnowledgePage from '@/app/knowledge/page';
import { apiClient } from '@/lib/api';
import { useAuth } from '@/lib/AuthProvider';
import { act, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('@/components/Header', () => ({
  Header: ({ title }: { title: string }) => <header>{title}</header>,
}));

vi.mock('@/lib/AuthProvider', () => ({
  useAuth: vi.fn().mockReturnValue({
    user: { id: '1', email: 'test@test.com', displayName: 'Test', role: 0 },
    loading: false,
    refresh: vi.fn(),
    logout: vi.fn(),
  }),
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
    Element.prototype.scrollIntoView = vi.fn();
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

  async function search(query: string) {
    fireEvent.change(screen.getByPlaceholderText('Search compounds, supplements, substances…'), { target: { value: query } });
    fireEvent.click(screen.getByRole('button', { name: 'Search', exact: true }));
    await waitFor(() => expect(screen.getByRole('button', { name: 'Search', exact: true })).toBeEnabled());
  }

  it.each([' BPC-157 ', ' Fixture-Alias ', ' pEpTiDe '])('matches trimmed query %j without changing displayed input or overlap guards', async query => {
    vi.mocked(apiClient.getAllKnowledgeCompounds).mockResolvedValue([{ ...knowledgeEntry, aliases: ['fixture-alias'] }]);
    render(<KnowledgePage />);
    await search(query);
    expect(screen.getByRole('button', { name: 'BPC-157', exact: true })).toBeVisible();
    expect(screen.getByPlaceholderText('Search compounds, supplements, substances…')).toHaveValue(query);
    expect(screen.getByRole('button', { name: 'Check Overlaps' })).toBeDisabled();
    expect(apiClient.getAllKnowledgeCompounds).toHaveBeenCalledTimes(1);
    expect(apiClient.checkOverlap).not.toHaveBeenCalled();
  });

  it('does not search whitespace-only input', () => {
    render(<KnowledgePage />);
    const input = screen.getByPlaceholderText('Search compounds, supplements, substances…');
    fireEvent.change(input, { target: { value: '   ' } });
    expect(screen.getByRole('button', { name: 'Search', exact: true })).toBeEnabled();
    fireEvent.submit(input.closest('form')!);
    expect(input).toHaveValue('   ');
    expect(apiClient.getAllKnowledgeCompounds).not.toHaveBeenCalled();
  });

  it('keeps selected names across searches and no results, supports focus and removal', async () => {
    vi.mocked(apiClient.getAllKnowledgeCompounds).mockResolvedValue([knowledgeEntry, { ...knowledgeEntry, canonicalName: 'Creatine' }]);
    render(<KnowledgePage />);
    expect(screen.getByText('Search for a compound and select it below.')).toBeVisible();
    await search('BPC');
    fireEvent.click(await screen.findByRole('button', { name: 'BPC-157', exact: true }));
    expect(screen.getByRole('button', { name: /BPC-157/, pressed: true })).toBeInTheDocument();
    await search('Creatine');
    expect(within(screen.getByRole('region', { name: 'Selected compounds' })).getByRole('button', { name: 'Remove BPC-157' })).toBeVisible();
    expect(screen.getByText('Select 1 more compound to check overlaps.')).toBeVisible();
    await search('no-match');
    expect(await screen.findByText(/No results for/)).toBeVisible();
    expect(screen.getByRole('button', { name: 'Remove BPC-157' })).toBeVisible();
    fireEvent.click(screen.getByRole('button', { name: 'Find another compound' }));
    expect(screen.getByPlaceholderText('Search compounds, supplements, substances…')).toHaveFocus();
    expect(screen.getByRole('button', { name: 'Remove BPC-157' })).toBeVisible();
    fireEvent.click(screen.getByRole('button', { name: 'Remove BPC-157' }));
    expect(screen.getByText('No compounds selected')).toBeVisible();
    expect(apiClient.checkOverlap).not.toHaveBeenCalled();
  });

  it('keeps unique selections, sends only the two canonical names, and clears', async () => {
    vi.mocked(apiClient.getAllKnowledgeCompounds).mockResolvedValue([knowledgeEntry, { ...knowledgeEntry, canonicalName: 'Creatine' }]);
    render(<KnowledgePage />);
    await search('BPC');
    fireEvent.click(await screen.findByRole('button', { name: 'BPC-157', exact: true }));
    fireEvent.click(screen.getByRole('button', { name: /BPC-157/, pressed: true }));
    fireEvent.click(screen.getByRole('button', { name: 'BPC-157', exact: true }));
    await search('Creatine');
    fireEvent.click(await screen.findByRole('button', { name: 'Creatine', exact: true }));
    expect(within(screen.getByRole('region', { name: 'Selected compounds' })).getAllByRole('button')).toHaveLength(2);
    expect(screen.getByText('Ready to check selected compounds.')).toBeVisible();
    expect(apiClient.checkOverlap).not.toHaveBeenCalled();
    fireEvent.click(screen.getByRole('button', { name: 'Check Overlaps' }));
    await waitFor(() => expect(apiClient.checkOverlap).toHaveBeenCalledWith(['BPC-157', 'Creatine']));
    await screen.findByText('No pathway overlaps detected for selected compounds.');
    fireEvent.click(screen.getByRole('button', { name: 'Clear', exact: true }));
    expect(screen.queryByRole('region', { name: 'Selected compounds' })).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Check Overlaps' })).toBeDisabled();
  });

  it('preserves pending disable and error retry behavior', async () => {
    vi.mocked(apiClient.getAllKnowledgeCompounds).mockResolvedValue([knowledgeEntry, { ...knowledgeEntry, canonicalName: 'Creatine' }]);
    let rejectRequest!: (error: Error) => void;
    vi.mocked(apiClient.checkOverlap).mockImplementationOnce(() => new Promise((_, reject) => { rejectRequest = reject; }));
    render(<KnowledgePage />);
    await search('BPC'); fireEvent.click(await screen.findByRole('button', { name: 'BPC-157', exact: true }));
    await search('Creatine'); fireEvent.click(await screen.findByRole('button', { name: 'Creatine', exact: true }));
    fireEvent.click(screen.getByRole('button', { name: 'Check Overlaps' }));
    expect(screen.getByRole('button', { name: 'Checking…' })).toBeDisabled();
    await act(async () => rejectRequest(new Error('fixture failure')));
    expect(screen.getByText('Failed to check overlaps')).toBeVisible();
    expect(screen.getByRole('button', { name: 'Check Overlaps' })).toBeEnabled();
    fireEvent.click(screen.getByRole('button', { name: 'Check Overlaps' }));
    await screen.findByText('No pathway overlaps detected for selected compounds.');
    expect(apiClient.checkOverlap).toHaveBeenCalledTimes(2);
    expect(screen.queryByText('Failed to check overlaps')).not.toBeInTheDocument();
  });

  it.each(['resolve', 'reject'])('ignores stale overlap %s after replacing a selected compound', async outcome => {
    vi.mocked(apiClient.getAllKnowledgeCompounds).mockResolvedValue([knowledgeEntry, { ...knowledgeEntry, canonicalName: 'Creatine' }, { ...knowledgeEntry, canonicalName: 'Fixture C' }]);
    let resolveOld!: (value: []) => void;
    let rejectOld!: (error: Error) => void;
    vi.mocked(apiClient.checkOverlap).mockImplementationOnce(() => new Promise((resolve, reject) => { resolveOld = resolve; rejectOld = reject; }));
    render(<KnowledgePage />);
    await search('BPC'); fireEvent.click(screen.getByRole('button', { name: 'BPC-157', exact: true }));
    await search('Creatine'); fireEvent.click(screen.getByRole('button', { name: 'Creatine', exact: true }));
    fireEvent.click(screen.getByRole('button', { name: 'Check Overlaps' }));
    expect(apiClient.checkOverlap).toHaveBeenCalledWith(['BPC-157', 'Creatine']);
    fireEvent.click(screen.getByRole('button', { name: 'Remove Creatine' }));
    await search('Fixture C'); fireEvent.click(screen.getByRole('button', { name: 'Fixture C', exact: true }));
    await act(async () => { if (outcome === 'resolve') resolveOld([]); else rejectOld(new Error('old failure')); });
    expect(screen.queryByText('No pathway overlaps detected for selected compounds.')).not.toBeInTheDocument();
    expect(screen.queryByText('Failed to check overlaps')).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Check Overlaps' })).toBeEnabled();
    expect(apiClient.checkOverlap).toHaveBeenCalledTimes(1);
  });

  it('does not let an old completion end a newer request after Clear and reselection', async () => {
    vi.mocked(apiClient.getAllKnowledgeCompounds).mockResolvedValue([knowledgeEntry, { ...knowledgeEntry, canonicalName: 'Creatine' }]);
    let resolveOld!: (value: []) => void;
    let resolveNew!: (value: []) => void;
    vi.mocked(apiClient.checkOverlap)
      .mockImplementationOnce(() => new Promise(resolve => { resolveOld = resolve; }))
      .mockImplementationOnce(() => new Promise(resolve => { resolveNew = resolve; }));
    render(<KnowledgePage />);
    await search('Peptide');
    fireEvent.click(screen.getByRole('button', { name: 'BPC-157', exact: true }));
    fireEvent.click(screen.getByRole('button', { name: 'Creatine', exact: true }));
    fireEvent.click(screen.getByRole('button', { name: 'Check Overlaps' }));
    fireEvent.click(screen.getByRole('button', { name: 'Clear', exact: true }));
    fireEvent.click(screen.getByRole('button', { name: 'BPC-157', exact: true }));
    fireEvent.click(screen.getByRole('button', { name: 'Creatine', exact: true }));
    fireEvent.click(screen.getByRole('button', { name: 'Check Overlaps' }));
    expect(apiClient.checkOverlap).toHaveBeenCalledTimes(2);
    await act(async () => resolveOld([]));
    expect(screen.getByRole('button', { name: 'Checking…' })).toBeDisabled();
    expect(screen.queryByText('No pathway overlaps detected for selected compounds.')).not.toBeInTheDocument();
    await act(async () => resolveNew([]));
    expect(screen.getByText('No pathway overlaps detected for selected compounds.')).toBeVisible();
    expect(screen.getByRole('button', { name: 'Check Overlaps' })).toBeEnabled();
    expect(apiClient.checkOverlap).toHaveBeenLastCalledWith(['BPC-157', 'Creatine']);
  });

  it('labels empty results with the completed query only', async () => {
    render(<KnowledgePage />);
    const input = screen.getByPlaceholderText('Search compounds, supplements, substances…');
    fireEvent.change(input, { target: { value: 'not-submitted' } });
    expect(screen.queryByText(/No results for/)).not.toBeInTheDocument();
    await search('absent');
    expect(screen.getByText('No results for “absent”')).toBeVisible();
    fireEvent.change(input, { target: { value: 'draft query' } });
    expect(screen.getByText('No results for “absent”')).toBeVisible();
  });

  it.each([false, true])('does not show empty or previous search results after transport failure (previous hits: %s)', async previousHits => {
    render(<KnowledgePage />);
    if (previousHits) await search('BPC');
    vi.mocked(apiClient.getAllKnowledgeCompounds).mockRejectedValueOnce(new Error('fixture transport failure'));
    await search('next query');
    expect(screen.getByRole('alert')).toHaveTextContent('Failed to search knowledge base');
    expect(screen.queryByText(/No results for/)).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'BPC-157', exact: true })).not.toBeInTheDocument();
  });

  it.each(['Clear', 'Remove Creatine'])('clears obsolete overlap errors when selection changes through %s', async control => {
    vi.mocked(apiClient.getAllKnowledgeCompounds).mockResolvedValue([knowledgeEntry, { ...knowledgeEntry, canonicalName: 'Creatine' }]);
    vi.mocked(apiClient.checkOverlap).mockRejectedValueOnce(new Error('fixture failure'));
    render(<KnowledgePage />);
    await search('BPC'); fireEvent.click(screen.getByRole('button', { name: 'BPC-157', exact: true }));
    await search('Creatine'); fireEvent.click(screen.getByRole('button', { name: 'Creatine', exact: true }));
    fireEvent.click(screen.getByRole('button', { name: 'Check Overlaps' }));
    await screen.findByText('Failed to check overlaps');
    fireEvent.click(screen.getByRole('button', { name: control, exact: true }));
    expect(screen.queryByText('Failed to check overlaps')).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Check Overlaps' })).toBeDisabled();
  });

  it('does not retain an earlier overlap success after a failed recheck', async () => {
    vi.mocked(apiClient.getAllKnowledgeCompounds).mockResolvedValue([knowledgeEntry, { ...knowledgeEntry, canonicalName: 'Creatine' }]);
    render(<KnowledgePage />);
    await search('BPC'); fireEvent.click(screen.getByRole('button', { name: 'BPC-157', exact: true }));
    await search('Creatine'); fireEvent.click(screen.getByRole('button', { name: 'Creatine', exact: true }));
    fireEvent.click(screen.getByRole('button', { name: 'Check Overlaps' }));
    await screen.findByText('No pathway overlaps detected for selected compounds.');
    vi.mocked(apiClient.checkOverlap).mockRejectedValueOnce(new Error('recheck failure'));
    fireEvent.click(screen.getByRole('button', { name: 'Check Overlaps' }));
    await screen.findByText('Failed to check overlaps');
    expect(screen.queryByText('No pathway overlaps detected for selected compounds.')).not.toBeInTheDocument();
    expect(screen.getByRole('alert')).toHaveTextContent('Failed to check overlaps');
  });

  it('renders marketing chrome instead of the in-app Header for anonymous visitors', () => {
    vi.mocked(useAuth).mockReturnValue({
      user: null,
      loading: false,
      refresh: vi.fn(),
      logout: vi.fn(),
    });

    render(<KnowledgePage />);

    expect(screen.queryByText('Knowledge Base')).not.toBeInTheDocument();
    expect(screen.getAllByText('Compounds & Evidence').length).toBeGreaterThan(0);
  });
});
