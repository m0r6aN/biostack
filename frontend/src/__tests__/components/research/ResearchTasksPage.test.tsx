import ResearchTasksPage from '@/app/admin/research/tasks/page';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';

const clipboardMock = vi.hoisted(() => ({ writeText: vi.fn() }));

const searchParamsMock = vi.hoisted(() => ({ value: new URLSearchParams() }));

vi.mock('next/navigation', () => ({
  useSearchParams: () => searchParamsMock.value,
}));

vi.mock('@/lib/apiBase', () => ({ getApiBaseUrl: () => 'http://localhost' }));

vi.mock('@/components/Header', () => ({
  Header: ({ title }: { title: string }) => <div>{title}</div>,
}));

vi.mock('@/components/ui/GlassCard', () => ({
  GlassCard: ({ children, className }: { children: React.ReactNode; className?: string }) => (
    <div className={className}>{children}</div>
  ),
}));

vi.mock('@/lib/research/loader', () => ({
  fetchResearchCategoryTaxonomy: vi.fn().mockResolvedValue({
    taxonomyVersion: '1.0.0',
    updatedAtUtc: '2026-05-10T00:00:00Z',
    categories: [
      { name: 'Nootropics', aliases: ['nootropic', 'cognitive support'] },
      { name: 'Longevity', aliases: ['longevity'] },
    ],
  }),
  fetchResearchTaskQueue: vi.fn().mockResolvedValue({
    queueVersion: '1.0.0',
    generatedAtUtc: '2026-05-10T00:00:00Z',
    counts: { totalItems: 2, urgent: 0, high: 1, normal: 1, low: 0, resolvedItems: 1 },
    items: [
      {
        taskId: 'epitalon-initial-research',
        taskType: 'generate-evidence-packet',
        compoundName: 'Epitalon',
        aliases: ['Epithalon'],
        categories: ['Longevity'],
        classification: 'Research Compound',
        priority: 'high',
        requestIds: ['research-1'],
        requesterIds: ['operator-1'],
        firstRequestedAtUtc: '',
        latestRequestedAtUtc: '',
        rationales: ['Investigate longevity protocol evidence.'],
        notes: ['Human data first.'],
        suggestedResearchDirectives: ['Use the source registry agent first.'],
        targetEvidencePath: 'research/input/evidence/epitalon.evidence.json',
        requiredSchema: 'evidence-packet.schema.json',
      },
      {
        taskId: 'mots-c-initial-research',
        taskType: 'generate-evidence-packet',
        compoundName: 'MOTS-c',
        aliases: [],
        categories: ['Nootropics'],
        classification: 'Peptide',
        priority: 'normal',
        requestIds: ['research-2'],
        requesterIds: ['operator-2'],
        firstRequestedAtUtc: '',
        latestRequestedAtUtc: '',
        rationales: ['Investigate mitochondrial signaling claims.'],
        notes: [],
        suggestedResearchDirectives: ['Do not fabricate evidence.'],
        targetEvidencePath: 'research/input/evidence/mots-c.evidence.json',
        requiredSchema: 'evidence-packet.schema.json',
      },
    ],
    resolvedItems: [
      {
        taskId: 'noopept-initial-research',
        compoundName: 'Noopept',
        aliases: ['GVS-111'],
        categories: ['Nootropics'],
        classification: 'Research Compound',
        priority: 'high',
        requestIds: ['research-3'],
        requesterIds: ['operator-1'],
        firstRequestedAtUtc: '',
        latestRequestedAtUtc: '',
        resolvedAtUtc: '',
        currentReadiness: 'review-required',
        resolution: 'evidence-detected',
        resolutionReason: 'Evidence is now present.',
        targetEvidencePath: 'research/input/evidence/noopept.evidence.json',
      },
    ],
  }),
}));

describe('ResearchTasksPage', () => {
  beforeEach(() => {
    searchParamsMock.value = new URLSearchParams();
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify({ token: 'dev' }), { status: 200 })));
    vi.stubGlobal('navigator', { clipboard: clipboardMock });
    clipboardMock.writeText.mockReset();
  });

  afterEach(() => vi.unstubAllGlobals());

  it('renders queued evidence tasks', async () => {
    render(<ResearchTasksPage />);

    expect(await screen.findByText('Research Tasks')).toBeInTheDocument();
    expect(screen.getByText('Epitalon')).toBeInTheDocument();
    expect(screen.getByText('MOTS-c')).toBeInTheDocument();
    expect(screen.getByText('Noopept')).toBeInTheDocument();
    expect(screen.getByText('research/input/evidence/epitalon.evidence.json')).toBeInTheDocument();
    expect(screen.getByText(/Use the source registry agent first/i)).toBeInTheDocument();
    expect(screen.getByText(/Consumed on the latest run/i)).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /Export filtered handoff/i })).toHaveAttribute('href', expect.stringContaining('Epitalon'));
  });

  it('focuses the queue on a compound when the query param is present', async () => {
    searchParamsMock.value = new URLSearchParams('compound=epitalon');

    render(<ResearchTasksPage />);

    expect(await screen.findByText(/Focused on Epitalon/i)).toBeInTheDocument();
    expect(screen.getByText('Epitalon')).toBeInTheDocument();
    expect(screen.queryByText('MOTS-c')).not.toBeInTheDocument();
    expect(screen.getByText('Clear focus')).toBeInTheDocument();
  });

  it('filters by category and copies a handoff payload', async () => {
    render(<ResearchTasksPage />);

    fireEvent.click(await screen.findByRole('button', { name: 'Nootropics' }));

    expect(screen.getByText('MOTS-c')).toBeInTheDocument();
    expect(screen.getByText('Noopept')).toBeInTheDocument();
    expect(screen.queryByText('Epitalon')).not.toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: /Copy handoff payload/i }));

    await waitFor(() => expect(clipboardMock.writeText).toHaveBeenCalledTimes(1));
    expect(clipboardMock.writeText.mock.calls[0][0]).toContain('Nootropics');
    expect(clipboardMock.writeText.mock.calls[0][0]).toContain('MOTS-c');
    expect(await screen.findByText(/Copied handoff payload for MOTS-c/i)).toBeInTheDocument();
  });

  it('offers a clear category action after selecting a preset', async () => {
    render(<ResearchTasksPage />);

    fireEvent.click(await screen.findByRole('button', { name: 'Nootropics' }));

    expect(screen.getByRole('button', { name: /Clear category/i })).toBeInTheDocument();
  });
});