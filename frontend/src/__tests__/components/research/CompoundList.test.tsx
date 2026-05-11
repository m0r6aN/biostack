import CompoundList from '@/app/admin/research/compounds/page';
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';

const loaderMocks = vi.hoisted(() => ({
  fetchResearchSummary: vi.fn().mockResolvedValue({
    draftSubstanceCount: 4, reviewQueueItemCount: 4, researchRequestCount: 1,
    compounds: [
      { name: 'Epitalon', classification: 'Research Compound', overallEvidenceTier: 'Unknown', completeness: 'requested', needsReview: true, reviewQueueItemCount: 0, promotionReadiness: 'research-requested', promotionBlockers: ['research-requested: evidence packet has not been generated'], reviewDecisionIds: [], hasResearchRequest: true, researchRequestIds: ['research-1'], qualityFlags: ['research-requested'], reviewReasons: [] },
      { name: 'BPC-157', classification: 'Peptide', overallEvidenceTier: 'Limited', completeness: 'partial', needsReview: true, reviewQueueItemCount: 3, promotionReadiness: 'blocked', promotionBlockers: ['blocked: missing authoritative support'], reviewDecisionIds: [], hasRequestedChanges: false, qualityFlags: [], reviewReasons: [] },
      { name: 'Semaglutide', classification: 'Pharmaceutical', overallEvidenceTier: 'Moderate', completeness: 'substantial', needsReview: true, reviewQueueItemCount: 1, promotionReadiness: 'review-required', promotionBlockers: ['review-required: requested changes pending re-review'], reviewDecisionIds: ['change-1'], hasRequestedChanges: true, qualityFlags: [], reviewReasons: [] },
      { name: 'Creatine', classification: 'Supplement', overallEvidenceTier: 'Strong', completeness: 'substantial', needsReview: false, reviewQueueItemCount: 0, promotionReadiness: 'candidate-for-promotion', promotionBlockers: [], reviewDecisionIds: [], hasRequestedChanges: false, qualityFlags: [], reviewReasons: [] },
    ],
    reviewCategories: [], promotionReadiness: [], qualityFlags: [], reviewReasons: [], classifications: [], evidenceTiers: [],
  }),
  fetchPromotionManifest: vi.fn().mockResolvedValue({
    manifestVersion: '1.0.0', generatedAtUtc: '',
    counts: { totalDrafts: 4, blocked: 1, reviewRequired: 1, researchRequested: 1, candidatesForPromotion: 1 },
    blocked: [], reviewRequired: [], researchRequested: [], candidatesForPromotion: [],
  }),
  fetchResearchTaskQueue: vi.fn().mockResolvedValue({
    queueVersion: '1.0.0', generatedAtUtc: '',
    counts: { totalItems: 1, urgent: 0, high: 1, normal: 0, low: 0 },
    items: [{
      taskId: 'epitalon-initial-research', taskType: 'generate-evidence-packet', compoundName: 'Epitalon', aliases: ['Epithalon'], categories: ['Longevity'], classification: 'Research Compound', priority: 'high',
      requestIds: ['research-1'], requesterIds: ['operator-1'], firstRequestedAtUtc: '', latestRequestedAtUtc: '',
      rationales: ['User requested coverage for longevity protocols.'], notes: [],
      suggestedResearchDirectives: ['Do not fabricate evidence.'], targetEvidencePath: 'research/input/evidence/epitalon.evidence.json', requiredSchema: 'evidence-packet.schema.json',
    }],
    resolvedItems: [],
  }),
  fetchResearchCategoryTaxonomy: vi.fn().mockResolvedValue({
    taxonomyVersion: '1.0.0',
    updatedAtUtc: '2026-05-10T00:00:00Z',
    categories: [
      { name: 'Nootropics', aliases: ['nootropic', 'cognitive support'] },
      { name: 'Anti-Aging', aliases: ['anti-aging'], deprecated: true, replacedBy: 'Longevity' },
      { name: 'Longevity', aliases: ['longevity'] },
      { name: 'Mitochondrial Support', aliases: ['mitochondrial support'] },
    ],
  }),
}));
const pushMock = vi.hoisted(() => vi.fn());

vi.mock('next/navigation', () => ({
  useRouter: () => ({ push: pushMock, replace: vi.fn() }),
  useSearchParams: () => new URLSearchParams(),
  usePathname: () => '/admin/research/compounds',
}));
vi.mock('@/lib/apiBase', () => ({ getApiBaseUrl: () => 'http://localhost' }));
vi.mock('@/lib/research/loader', () => ({
  fetchResearchSummary: () => loaderMocks.fetchResearchSummary(),
  fetchPromotionManifest: () => loaderMocks.fetchPromotionManifest(),
  fetchResearchTaskQueue: () => loaderMocks.fetchResearchTaskQueue(),
  fetchResearchCategoryTaxonomy: () => loaderMocks.fetchResearchCategoryTaxonomy(),
}));
vi.mock('@/components/Header', () => ({
  Header: ({ title }: { title: string }) => <div>{title}</div>,
}));

describe('CompoundList', () => {
  beforeEach(() => {
    pushMock.mockReset();
    loaderMocks.fetchResearchSummary.mockClear();
    loaderMocks.fetchPromotionManifest.mockClear();
    loaderMocks.fetchResearchTaskQueue.mockClear();
    loaderMocks.fetchResearchCategoryTaxonomy.mockClear();
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify({ token: 'dev' }), { status: 200 })));
  });

  afterEach(() => vi.unstubAllGlobals());

  it('renders compound names after loading', async () => {
    render(<CompoundList />);
    expect(await screen.findByText('Epitalon')).toBeInTheDocument();
    expect(await screen.findByText('BPC-157')).toBeInTheDocument();
    expect(await screen.findByText('Semaglutide')).toBeInTheDocument();
    expect(await screen.findByText('Creatine')).toBeInTheDocument();
  });

  it('renders filter bar with readiness chips', async () => {
    render(<CompoundList />);
    expect(await screen.findByText('Research Requested (1)')).toBeInTheDocument();
    expect(await screen.findByText('Blocked (1)')).toBeInTheDocument();
    expect(await screen.findByText('Candidate (1)')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Anti-Aging' })).not.toBeInTheDocument();
  });

  it('splits compounds into research, review, re-review, and processing lanes', async () => {
    render(<CompoundList />);
    expect(await screen.findByRole('heading', { name: 'Research Requested' })).toBeInTheDocument();
    expect(await screen.findByRole('heading', { name: 'Ready for Review' })).toBeInTheDocument();
    expect(await screen.findByRole('heading', { name: 'Ready for Re-review' })).toBeInTheDocument();
    expect(await screen.findByRole('heading', { name: 'Ready for Processing' })).toBeInTheDocument();
    expect(screen.getByText(/New compounds queued for initial evidence research/i)).toBeInTheDocument();
    expect(screen.getByText(/1 evidence task queued for agent pickup/i)).toBeInTheDocument();
    expect(screen.getByText(/Blocked or review-required drafts/i)).toBeInTheDocument();
    expect(screen.getByText(/Requested changes have been recorded/i)).toBeInTheDocument();
    expect(screen.getByText(/Candidates cleared for the next worker/i)).toBeInTheDocument();
  });

  it('submits a new research request', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(new Response(JSON.stringify({ token: 'dev' }), { status: 200 }))
      .mockResolvedValueOnce(new Response(JSON.stringify({ savedFilename: 'research-request-epitalon.json', exitCode: 0 }), { status: 200 }));
    vi.stubGlobal('fetch', fetchMock);

    render(<CompoundList />);
    fireEvent.change(await screen.findByPlaceholderText('Compound name'), { target: { value: 'MOTS-c' } });
    fireEvent.change(screen.getByPlaceholderText('Requester ID'), { target: { value: 'operator-7' } });
    fireEvent.change(screen.getByDisplayValue('Priority: normal'), { target: { value: 'high' } });
    fireEvent.click(await screen.findByRole('button', { name: 'Nootropics' }));
    fireEvent.change(screen.getByPlaceholderText(/Categories \(comma-separated/i), { target: { value: 'Nootropics, longevity' } });
    fireEvent.change(screen.getByPlaceholderText(/Why should BioStack research it/i), { target: { value: 'User asked about mitochondrial peptide support.' } });
    fireEvent.change(screen.getByPlaceholderText(/Optional notes for the evidence agent/i), { target: { value: 'Start with human cognition sources.' } });
    fireEvent.click(screen.getByRole('button', { name: /Queue Research Request/i }));

    expect(screen.getAllByText('Nootropics').length).toBeGreaterThan(0);
    expect(screen.getAllByText('Longevity').length).toBeGreaterThan(0);
    await waitFor(() => expect(fetchMock).toHaveBeenCalledWith('/api/research/request-compound', expect.objectContaining({ method: 'POST' })));
    expect(fetchMock).toHaveBeenCalledWith('/api/research/request-compound', expect.objectContaining({
      body: JSON.stringify({
        compoundName: 'MOTS-c',
        rationale: 'User asked about mitochondrial peptide support.',
        notes: 'Start with human cognition sources.',
        categories: 'Nootropics, longevity',
        requesterId: 'operator-7',
        priority: 'high',
      }),
    }));
    expect(await screen.findByText(/Queued research-request-epitalon.json/i)).toBeInTheDocument();
  });

  it('opens the task board for a queued research-requested compound', async () => {
    render(<CompoundList />);

    const card = (await screen.findByText('Epitalon')).closest('[role="button"]');
    expect(card).not.toBeNull();
    fireEvent.click(within(card as HTMLElement).getByRole('button', { name: /Open Task Board/i }));

    expect(pushMock).toHaveBeenCalledWith('/admin/research/tasks?compound=epitalon');
  });
});
