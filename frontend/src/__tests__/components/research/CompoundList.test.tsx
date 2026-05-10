import CompoundList from '@/app/admin/research/compounds/page';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';

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
}));

vi.mock('next/navigation', () => ({
  useRouter: () => ({ push: vi.fn(), replace: vi.fn() }),
  useSearchParams: () => new URLSearchParams(),
  usePathname: () => '/admin/research/compounds',
}));
vi.mock('@/lib/apiBase', () => ({ getApiBaseUrl: () => 'http://localhost' }));
vi.mock('@/lib/research/loader', () => ({
  fetchResearchSummary: () => loaderMocks.fetchResearchSummary(),
  fetchPromotionManifest: () => loaderMocks.fetchPromotionManifest(),
}));
vi.mock('@/components/Header', () => ({
  Header: ({ title }: { title: string }) => <div>{title}</div>,
}));

describe('CompoundList', () => {
  beforeEach(() => {
    loaderMocks.fetchResearchSummary.mockClear();
    loaderMocks.fetchPromotionManifest.mockClear();
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
  });

  it('splits compounds into research, review, re-review, and processing lanes', async () => {
    render(<CompoundList />);
    expect(await screen.findByRole('heading', { name: 'Research Requested' })).toBeInTheDocument();
    expect(await screen.findByRole('heading', { name: 'Ready for Review' })).toBeInTheDocument();
    expect(await screen.findByRole('heading', { name: 'Ready for Re-review' })).toBeInTheDocument();
    expect(await screen.findByRole('heading', { name: 'Ready for Processing' })).toBeInTheDocument();
    expect(screen.getByText(/New compounds queued for initial evidence research/i)).toBeInTheDocument();
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
    fireEvent.change(screen.getByPlaceholderText(/Why should BioStack research it/i), { target: { value: 'User asked about mitochondrial peptide support.' } });
    fireEvent.click(screen.getByRole('button', { name: /Queue Research Request/i }));

    await waitFor(() => expect(fetchMock).toHaveBeenCalledWith('/api/research/request-compound', expect.objectContaining({ method: 'POST' })));
    expect(await screen.findByText(/Queued research-request-epitalon.json/i)).toBeInTheDocument();
  });
});
