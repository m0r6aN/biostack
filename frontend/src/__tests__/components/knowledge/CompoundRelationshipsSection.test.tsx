import { render, screen, waitFor } from '@testing-library/react';
import { describe, expect, it, vi, beforeEach } from 'vitest';
import type { CompoundGraph } from '@/lib/research/types';
import { CompoundRelationshipsSection } from '@/components/knowledge/CompoundRelationshipsSection';

// --- Auth mock ---
vi.mock('@/lib/AuthProvider', () => ({
  useAuth: vi.fn(),
}));

// --- Graph loader mock ---
vi.mock('@/lib/research/loader', () => ({
  fetchCompoundGraph: vi.fn(),
}));

// --- Next.js Link mock ---
vi.mock('next/link', () => ({
  default: ({ href, children, ...props }: { href: string; children: React.ReactNode }) => (
    <a href={href} {...props}>{children}</a>
  ),
}));

import { useAuth } from '@/lib/AuthProvider';
import { fetchCompoundGraph } from '@/lib/research/loader';

const mockUseAuth = vi.mocked(useAuth);
const mockFetchGraph = vi.mocked(fetchCompoundGraph);

function makeGraph(overrides: Partial<CompoundGraph> = {}): CompoundGraph {
  return {
    graphVersion: '1.0.0',
    generatedAtUtc: '2026-05-16T00:00:00Z',
    counts: { nodes: 0, edges: 0, reviewRequiredEdges: 0, communitySignalEdges: 0, conflictEdges: 0 },
    nodes: [],
    edges: [],
    reviewFindings: [],
    ...overrides,
  };
}

function makeNode(overrides = {}) {
  return {
    nodeId: 'compound:creatine',
    nodeType: 'compound' as const,
    label: 'Creatine',
    aliases: [],
    metadata: {},
    ...overrides,
  };
}

function makeEdge(overrides = {}) {
  return {
    edgeId: 'e-1',
    from: 'compound:creatine',
    to: 'compound:beta-alanine',
    edgeType: 'synergizes-with',
    relationshipType: null,
    assertedRelationshipType: null,
    effectDomain: null,
    evidenceTier: 'Moderate',
    confidence: 'moderate',
    sourceRefs: [],
    claimRefs: [],
    reviewFlags: [],
    needsReview: false,
    communitySignal: null,
    sourceAuthorityMix: { authorityTiers: ['B'] },
    ...overrides,
  };
}

beforeEach(() => {
  mockUseAuth.mockReturnValue({
    user: { id: '1', email: 'test@test.com', displayName: 'Test', role: 0 },
    loading: false,
    refresh: vi.fn(),
    logout: vi.fn(),
  });
});

describe('CompoundRelationshipsSection', () => {
  it('renders null and does not call fetchCompoundGraph when user is null (unauthenticated)', async () => {
    mockUseAuth.mockReturnValue({ user: null, loading: false, refresh: vi.fn(), logout: vi.fn() });
    const { container } = render(
      <CompoundRelationshipsSection compoundName="Creatine" />
    );
    await waitFor(() => {
      expect(mockFetchGraph).not.toHaveBeenCalled();
    });
    expect(container.firstChild).toBeNull();
  });

  it('renders null when fetchCompoundGraph throws', async () => {
    mockFetchGraph.mockRejectedValue(new Error('404'));
    const { container } = render(
      <CompoundRelationshipsSection compoundName="Creatine" />
    );
    await waitFor(() => {
      expect(container.firstChild).toBeNull();
    });
  });

  it('renders null when graph has no matching relationship edges', async () => {
    mockFetchGraph.mockResolvedValue(makeGraph());
    const { container } = render(
      <CompoundRelationshipsSection compoundName="Creatine" />
    );
    await waitFor(() => {
      expect(container.firstChild).toBeNull();
    });
  });

  it('filters out taxonomic edges (belongs-to-category, affects-pathway)', async () => {
    mockFetchGraph.mockResolvedValue(makeGraph({
      nodes: [
        makeNode({ nodeId: 'compound:creatine', label: 'Creatine', nodeType: 'compound' }),
        makeNode({ nodeId: 'category:performance', label: 'Performance', nodeType: 'category' }),
      ],
      edges: [
        makeEdge({ from: 'compound:creatine', to: 'category:performance', edgeType: 'belongs-to-category' }),
      ],
    }));
    const { container } = render(
      <CompoundRelationshipsSection compoundName="Creatine" />
    );
    await waitFor(() => {
      expect(container.firstChild).toBeNull();
    });
  });

  it('matches compound by canonical name', async () => {
    mockFetchGraph.mockResolvedValue(makeGraph({
      nodes: [
        makeNode({ nodeId: 'compound:creatine', label: 'Creatine', nodeType: 'compound' }),
        makeNode({ nodeId: 'compound:beta-alanine', label: 'Beta-Alanine', nodeType: 'compound' }),
      ],
      edges: [makeEdge({ from: 'compound:creatine', to: 'compound:beta-alanine', edgeType: 'synergizes-with' })],
    }));
    render(<CompoundRelationshipsSection compoundName="Creatine" />);
    await waitFor(() => {
      expect(screen.getByText('Beta-Alanine')).toBeInTheDocument();
    });
  });

  it('matches compound by alias', async () => {
    mockFetchGraph.mockResolvedValue(makeGraph({
      nodes: [
        makeNode({ nodeId: 'compound:creatine', label: 'Creatine', nodeType: 'compound' }),
        makeNode({ nodeId: 'compound:beta-alanine', label: 'Beta-Alanine', nodeType: 'compound' }),
      ],
      edges: [makeEdge({ from: 'compound:creatine', to: 'compound:beta-alanine', edgeType: 'synergizes-with' })],
    }));
    render(
      <CompoundRelationshipsSection
        compoundName="Some Other Name"
        aliases={['Creatine Monohydrate', 'Creatine']}
      />
    );
    await waitFor(() => {
      expect(screen.getByText('Beta-Alanine')).toBeInTheDocument();
    });
  });

  it('matches compound by normalized node ID with compound: prefix stripped', async () => {
    mockFetchGraph.mockResolvedValue(makeGraph({
      nodes: [
        makeNode({ nodeId: 'compound:bpc-157', label: 'BPC-157', nodeType: 'compound' }),
        makeNode({ nodeId: 'compound:tb-500', label: 'TB-500', nodeType: 'compound' }),
      ],
      edges: [makeEdge({ from: 'compound:bpc-157', to: 'compound:tb-500', edgeType: 'complements' })],
    }));
    render(<CompoundRelationshipsSection compoundName="BPC-157" />);
    await waitFor(() => {
      expect(screen.getByText('TB-500')).toBeInTheDocument();
    });
  });

  it('renders user-facing labels — no raw edge type strings in DOM', async () => {
    mockFetchGraph.mockResolvedValue(makeGraph({
      nodes: [
        makeNode({ nodeId: 'compound:creatine', label: 'Creatine', nodeType: 'compound' }),
        makeNode({ nodeId: 'compound:beta-alanine', label: 'Beta-Alanine', nodeType: 'compound' }),
      ],
      edges: [makeEdge({ from: 'compound:creatine', to: 'compound:beta-alanine', edgeType: 'synergizes-with', evidenceTier: 'Strong' })],
    }));
    render(<CompoundRelationshipsSection compoundName="Creatine" />);
    await waitFor(() => {
      expect(screen.getByText('May work well together')).toBeInTheDocument();
      expect(screen.queryByText('synergizes-with')).not.toBeInTheDocument();
      expect(screen.queryByText('synergizeswith')).not.toBeInTheDocument();
    });
  });

  it('renders "Community report: not clinically verified" for Anecdotal evidence tier', async () => {
    mockFetchGraph.mockResolvedValue(makeGraph({
      nodes: [
        makeNode({ nodeId: 'compound:creatine', label: 'Creatine', nodeType: 'compound' }),
        makeNode({ nodeId: 'compound:beta-alanine', label: 'Beta-Alanine', nodeType: 'compound' }),
      ],
      edges: [makeEdge({ edgeType: 'has-community-signal', evidenceTier: 'Anecdotal' })],
    }));
    render(<CompoundRelationshipsSection compoundName="Creatine" />);
    await waitFor(() => {
      expect(screen.getByText('Community report: not clinically verified')).toBeInTheDocument();
    });
  });

  it('renders community signal badge for recurring and widespread; hides for none/absent', async () => {
    mockFetchGraph.mockResolvedValue(makeGraph({
      nodes: [
        makeNode({ nodeId: 'compound:creatine', label: 'Creatine', nodeType: 'compound' }),
        makeNode({ nodeId: 'compound:beta-alanine', label: 'Beta-Alanine', nodeType: 'compound' }),
        makeNode({ nodeId: 'compound:bpc-157', label: 'BPC-157', nodeType: 'compound' }),
      ],
      edges: [
        makeEdge({
          edgeId: 'e-rec',
          from: 'compound:creatine',
          to: 'compound:beta-alanine',
          edgeType: 'synergizes-with',
          communitySignal: { present: true, signalStrength: 'recurring', signalDirection: 'positive', canonicalTruthStatus: 'partially-supported' },
        }),
        makeEdge({
          edgeId: 'e-none',
          from: 'compound:creatine',
          to: 'compound:bpc-157',
          edgeType: 'synergizes-with',
          communitySignal: { present: false, signalStrength: 'none', signalDirection: 'unclear', canonicalTruthStatus: 'unknown' },
        }),
      ],
    }));
    render(<CompoundRelationshipsSection compoundName="Creatine" />);
    await waitFor(() => {
      expect(screen.getByText('Commonly reported in community')).toBeInTheDocument();
      expect(screen.queryByText('Rarely reported in community')).not.toBeInTheDocument();
    });
  });

  it('renders "Awaiting research review · Advisory signal only" for needsReview: true', async () => {
    mockFetchGraph.mockResolvedValue(makeGraph({
      nodes: [
        makeNode({ nodeId: 'compound:creatine', label: 'Creatine', nodeType: 'compound' }),
        makeNode({ nodeId: 'compound:beta-alanine', label: 'Beta-Alanine', nodeType: 'compound' }),
      ],
      edges: [makeEdge({ needsReview: true })],
    }));
    render(<CompoundRelationshipsSection compoundName="Creatine" />);
    await waitFor(() => {
      expect(screen.getByText('Awaiting research review · Advisory signal only')).toBeInTheDocument();
    });
  });

  it('does not render raw backend terms in the DOM', async () => {
    mockFetchGraph.mockResolvedValue(makeGraph({
      nodes: [
        makeNode({ nodeId: 'compound:creatine', label: 'Creatine', nodeType: 'compound' }),
        makeNode({ nodeId: 'compound:beta-alanine', label: 'Beta-Alanine', nodeType: 'compound' }),
      ],
      edges: [makeEdge({
        assertedRelationshipType: 'synergy',
        reviewFlags: ['low-authority-relationship-claim'],
        sourceRefs: ['src-001'],
        claimRefs: ['claim-001'],
        sourceAuthorityMix: { authorityTiers: ['D'] },
        communitySignal: {
          present: true,
          signalStrength: 'recurring',
          signalDirection: 'positive',
          canonicalTruthStatus: 'partially-supported',
        },
      })],
    }));
    render(<CompoundRelationshipsSection compoundName="Creatine" />);
    await waitFor(() => {
      expect(screen.queryByText(/assertedRelationshipType/)).not.toBeInTheDocument();
      expect(screen.queryByText(/sourceAuthorityMix/)).not.toBeInTheDocument();
      expect(screen.queryByText(/canonicalTruthStatus/)).not.toBeInTheDocument();
      expect(screen.queryByText(/partially-supported/)).not.toBeInTheDocument();
      expect(screen.queryByText(/reviewFlags/)).not.toBeInTheDocument();
      expect(screen.queryByText(/low-authority-relationship-claim/)).not.toBeInTheDocument();
      expect(screen.queryByText(/sourceRefs/)).not.toBeInTheDocument();
      expect(screen.queryByText(/src-001/)).not.toBeInTheDocument();
      expect(screen.queryByText(/claimRefs/)).not.toBeInTheDocument();
      expect(screen.queryByText(/claim-001/)).not.toBeInTheDocument();
    });
  });

  it('links counterpart only when nodeType is compound and toSlug produces a non-empty slug', async () => {
    mockFetchGraph.mockResolvedValue(makeGraph({
      nodes: [
        makeNode({ nodeId: 'compound:creatine', label: 'Creatine', nodeType: 'compound' }),
        makeNode({ nodeId: 'compound:beta-alanine', label: 'Beta-Alanine', nodeType: 'compound' }),
        makeNode({ nodeId: 'category:performance', label: 'Performance', nodeType: 'category' }),
      ],
      edges: [
        makeEdge({ edgeId: 'e-1', from: 'compound:creatine', to: 'compound:beta-alanine', edgeType: 'synergizes-with' }),
      ],
    }));
    render(<CompoundRelationshipsSection compoundName="Creatine" />);
    await waitFor(() => {
      const link = screen.getByRole('link', { name: 'Beta-Alanine' });
      expect(link).toHaveAttribute('href', '/knowledge/beta-alanine');
    });
  });

  it('sorts by evidence tier descending, then needsReview descending, then label alphabetically', async () => {
    mockFetchGraph.mockResolvedValue(makeGraph({
      nodes: [
        makeNode({ nodeId: 'compound:creatine', label: 'Creatine', nodeType: 'compound' }),
        makeNode({ nodeId: 'compound:beta-alanine', label: 'Beta-Alanine', nodeType: 'compound' }),
        makeNode({ nodeId: 'compound:bpc-157', label: 'BPC-157', nodeType: 'compound' }),
        makeNode({ nodeId: 'compound:tb-500', label: 'TB-500', nodeType: 'compound' }),
      ],
      edges: [
        makeEdge({ edgeId: 'e-1', from: 'compound:creatine', to: 'compound:bpc-157', edgeType: 'synergizes-with', evidenceTier: 'Limited', needsReview: false }),
        makeEdge({ edgeId: 'e-2', from: 'compound:creatine', to: 'compound:beta-alanine', edgeType: 'synergizes-with', evidenceTier: 'Strong', needsReview: false }),
        makeEdge({ edgeId: 'e-3', from: 'compound:creatine', to: 'compound:tb-500', edgeType: 'synergizes-with', evidenceTier: 'Limited', needsReview: true }),
      ],
    }));
    render(<CompoundRelationshipsSection compoundName="Creatine" />);
    await waitFor(() => {
      const labels = screen.getAllByRole('listitem').map(li => li.textContent ?? '');
      // Beta-Alanine first (Strong), then TB-500 (Limited + needsReview), then BPC-157 (Limited)
      expect(labels[0]).toContain('Beta-Alanine');
      expect(labels[1]).toContain('TB-500');
      expect(labels[2]).toContain('BPC-157');
    });
  });

  it('skips malformed/partial edges without crashing and renders remaining valid edges', async () => {
    mockFetchGraph.mockResolvedValue(makeGraph({
      nodes: [
        makeNode({ nodeId: 'compound:creatine', label: 'Creatine', nodeType: 'compound' }),
        makeNode({ nodeId: 'compound:beta-alanine', label: 'Beta-Alanine', nodeType: 'compound' }),
      ],
      edges: [
        // Malformed: missing 'to' field
        { edgeId: 'e-bad', from: 'compound:creatine', edgeType: 'synergizes-with' } as never,
        // Valid
        makeEdge({ edgeId: 'e-good', from: 'compound:creatine', to: 'compound:beta-alanine', edgeType: 'synergizes-with' }),
      ],
    }));
    render(<CompoundRelationshipsSection compoundName="Creatine" />);
    await waitFor(() => {
      expect(screen.getByText('Beta-Alanine')).toBeInTheDocument();
    });
  });
});
