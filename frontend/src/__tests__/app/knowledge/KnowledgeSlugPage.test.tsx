import { render, screen, waitFor } from '@testing-library/react';
import { describe, expect, it, vi, beforeEach } from 'vitest';

// --- Auth mock ---
vi.mock('@/lib/AuthProvider', () => ({
  useAuth: vi.fn().mockReturnValue({
    user: { id: '1', email: 'test@test.com', displayName: 'Test', role: 0 },
    loading: false,
    refresh: vi.fn(),
    logout: vi.fn(),
  }),
}));

// --- API client mock ---
vi.mock('@/lib/api', () => ({
  apiClient: {
    getKnowledgeEntry: vi.fn(),
  },
}));

// --- CompoundIntelligenceCard mock ---
vi.mock('@/components/knowledge/CompoundIntelligenceCard', () => ({
  CompoundIntelligenceCard: ({ entry }: { entry: { canonicalName: string } }) => (
    <div data-testid="compound-intelligence-card">{entry.canonicalName}</div>
  ),
}));

// --- CompoundRelationshipsSection mock (avoid real graph fetch in page tests) ---
vi.mock('@/components/knowledge/CompoundRelationshipsSection', () => ({
  CompoundRelationshipsSection: () => <div data-testid="relationships-section" />,
}));

// --- Next.js Link mock ---
vi.mock('next/link', () => ({
  default: ({ href, children }: { href: string; children: React.ReactNode }) => (
    <a href={href}>{children}</a>
  ),
}));

import { apiClient } from '@/lib/api';
import KnowledgeSlugPage from '@/app/knowledge/[slug]/page';

const mockGetEntry = vi.mocked(apiClient.getKnowledgeEntry);

const mockEntry = {
  canonicalName: 'Creatine',
  aliases: ['Creatine Monohydrate'],
  classification: 'Performance Supplement',
  evidenceTier: 'Strong',
  mechanismSummary: 'Increases phosphocreatine stores.',
  effects: [],
  pathways: [],
  benefits: [],
  pairsWellWith: [],
  avoidWith: [],
  drugInteractions: [],
  regulatoryStatus: 'Legal',
  recommendedDosage: null,
  standardDosageRange: null,
  maxReportedDose: null,
  tieredDosing: [],
  optimizationProtein: null,
  optimizationCarbs: null,
  optimizationSleep: null,
  optimizationExercise: null,
  optimizationSupplements: [],
};

describe('/knowledge/[slug] page', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders CompoundIntelligenceCard when getKnowledgeEntry resolves', async () => {
    mockGetEntry.mockResolvedValue(mockEntry);
    render(<KnowledgeSlugPage params={{ slug: 'creatine' }} />);
    await waitFor(() => {
      expect(screen.getByTestId('compound-intelligence-card')).toBeInTheDocument();
      expect(screen.getByText('Creatine')).toBeInTheDocument();
    });
  });

  it('does not reference getTrustLedger, TrustLedgerResponse, or CompoundDossier', () => {
    // Verify getKnowledgeEntry is the active method (not getTrustLedger)
    expect(mockGetEntry).toBeDefined();
    expect(apiClient).not.toHaveProperty('getTrustLedger');
  });

  it('shows skeleton while loading', () => {
    mockGetEntry.mockReturnValue(new Promise(() => {})); // never resolves
    render(<KnowledgeSlugPage params={{ slug: 'creatine' }} />);
    // DossierSkeleton renders animate-pulse cards
    expect(document.querySelectorAll('.animate-pulse').length).toBeGreaterThan(0);
  });

  it('shows rose error card when getKnowledgeEntry rejects', async () => {
    mockGetEntry.mockRejectedValue(new Error('Failed to load compound dossier.'));
    render(<KnowledgeSlugPage params={{ slug: 'creatine' }} />);
    await waitFor(() => {
      expect(screen.getByText('Failed to load compound dossier.')).toBeInTheDocument();
    });
  });

  it('does not render CompoundRelationshipsSection before entry loads', () => {
    mockGetEntry.mockReturnValue(new Promise(() => {})); // never resolves
    render(<KnowledgeSlugPage params={{ slug: 'creatine' }} />);
    expect(screen.queryByTestId('relationships-section')).not.toBeInTheDocument();
  });
});
