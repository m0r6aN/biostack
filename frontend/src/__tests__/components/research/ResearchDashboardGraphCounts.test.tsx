import ResearchDashboard from '@/app/admin/research/page';
import { render, screen } from '@testing-library/react';

vi.mock('next/link', () => ({
  default: ({ href, children, ...props }: React.AnchorHTMLAttributes<HTMLAnchorElement>) => (
    <a href={href} {...props}>{children}</a>
  ),
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
  fetchResearchSummary: vi.fn().mockResolvedValue({
    draftSubstanceCount: 0,
    reviewQueueItemCount: 0,
    compounds: [],
    reviewCategories: [],
    promotionReadiness: [],
    qualityFlags: [], reviewReasons: [], classifications: [], evidenceTiers: [],
  }),
  fetchPromotionManifest: vi.fn().mockResolvedValue({
    manifestVersion: '1.0.0', generatedAtUtc: '',
    counts: { totalDrafts: 0, blocked: 0, reviewRequired: 0, candidatesForPromotion: 0 },
    blocked: [], reviewRequired: [], candidatesForPromotion: [],
  }),
  fetchReviewResolutionPlan: vi.fn().mockResolvedValue({
    planVersion: '1.0.0', generatedAtUtc: '',
    counts: { totalItems: 0, blockedItems: 0, reviewRequiredItems: 0, resolutionTypes: [] },
    items: [],
  }),
  fetchImportPreview: vi.fn().mockResolvedValue(null),
  fetchDryRunReport: vi.fn().mockResolvedValue(null),
  fetchCompoundGraph: vi.fn().mockResolvedValue({
    graphVersion: '1.0.0',
    generatedAtUtc: '2026-05-05T09:00:00Z',
    counts: {
      nodes: 5,
      edges: 7,
      reviewRequiredEdges: 2,
      communitySignalEdges: 3,
      conflictEdges: 1,
    },
    nodes: [],
    edges: [],
    reviewFindings: [],
  }),
}));

describe('ResearchDashboard — Relationship Graph counts row', () => {
  it('renders the Relationship Graph heading', async () => {
    render(<ResearchDashboard />);
    expect(await screen.findByText(/Relationship Graph/i)).toBeInTheDocument();
  });

  it('renders edge counts, review-required, community-signal, and conflicts', async () => {
    render(<ResearchDashboard />);
    expect(await screen.findByText(/relationships/i)).toBeInTheDocument();
    // The numbers from the graph fixture
    expect(await screen.findByText('7')).toBeInTheDocument();
    expect(await screen.findByText('2')).toBeInTheDocument();
    expect(await screen.findByText('3')).toBeInTheDocument();
    expect(await screen.findByText('1')).toBeInTheDocument();
    expect(screen.getByText(/review-required/i)).toBeInTheDocument();
    expect(screen.getByText(/community-signal/i)).toBeInTheDocument();
    expect(screen.getByText(/conflicts/i)).toBeInTheDocument();
  });
});
