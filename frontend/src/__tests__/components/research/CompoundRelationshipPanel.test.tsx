import { CompoundRelationshipPanel } from '@/components/research/CompoundRelationshipPanel';
import type {
  CompoundGraph,
  CompoundGraphEdge,
  CompoundGraphNode,
  CompoundGraphReviewFinding,
} from '@/lib/research/types';
import { render, screen } from '@testing-library/react';

vi.mock('next/link', () => ({
  default: ({ href, children, ...props }: React.AnchorHTMLAttributes<HTMLAnchorElement>) => (
    <a href={href} {...props}>{children}</a>
  ),
}));

vi.mock('@/components/ui/GlassCard', () => ({
  GlassCard: ({ children, className }: { children: React.ReactNode; className?: string }) => (
    <div className={className}>{children}</div>
  ),
}));

function makeNode(overrides: Partial<CompoundGraphNode> & { nodeId: string; label: string }): CompoundGraphNode {
  return {
    nodeType: 'compound',
    aliases: [],
    metadata: {},
    ...overrides,
  };
}

function makeEdge(overrides: Partial<CompoundGraphEdge> & {
  edgeId: string;
  from: string;
  to: string;
  edgeType: CompoundGraphEdge['edgeType'];
}): CompoundGraphEdge {
  return {
    relationshipType: null,
    assertedRelationshipType: null,
    effectDomain: null,
    evidenceTier: null,
    confidence: null,
    sourceRefs: [],
    claimRefs: [],
    reviewFlags: [],
    needsReview: false,
    communitySignal: null,
    sourceAuthorityMix: { authorityTiers: [] },
    ...overrides,
  };
}

const baseGraph: CompoundGraph = {
  graphVersion: '1.0.0',
  generatedAtUtc: '2026-05-05T09:00:00Z',
  counts: {
    nodes: 0, edges: 0, reviewRequiredEdges: 0, communitySignalEdges: 0, conflictEdges: 0,
  },
  nodes: [],
  edges: [],
  reviewFindings: [],
};

describe('CompoundRelationshipPanel', () => {
  it('renders empty state when graph is null', () => {
    render(
      <CompoundRelationshipPanel
        graph={null}
        compound={{ canonicalName: 'Creatine', slug: 'creatine' }}
      />
    );
    expect(screen.getByText(/No cross-compound relationships recorded/i)).toBeInTheDocument();
  });

  it('renders empty state when there are no relationship edges for this compound', () => {
    const graph: CompoundGraph = {
      ...baseGraph,
      nodes: [
        makeNode({ nodeId: 'compound:creatine', label: 'Creatine' }),
        makeNode({ nodeId: 'compound:bpc-157', label: 'BPC-157' }),
      ],
      edges: [
        // unrelated edge between two other compounds
        makeEdge({
          edgeId: 'edge:unrelated',
          from: 'compound:other-a',
          to: 'compound:other-b',
          edgeType: 'synergizes-with',
        }),
        // taxonomic edge should not count even if it touches this compound
        makeEdge({
          edgeId: 'edge:cat',
          from: 'compound:creatine',
          to: 'category:performance',
          edgeType: 'belongs-to-category',
        }),
      ],
    };
    render(
      <CompoundRelationshipPanel
        graph={graph}
        compound={{ canonicalName: 'Creatine', slug: 'creatine' }}
      />
    );
    expect(screen.getByText(/No cross-compound relationships recorded/i)).toBeInTheDocument();
  });

  it('renders edges where the current compound is the "from" endpoint (canonical name match)', () => {
    const graph: CompoundGraph = {
      ...baseGraph,
      nodes: [
        makeNode({ nodeId: 'n:cre', label: 'Creatine' }),
        makeNode({ nodeId: 'n:ba', label: 'Beta-Alanine' }),
      ],
      edges: [
        makeEdge({
          edgeId: 'edge:cre-syn-ba',
          from: 'n:cre',
          to: 'n:ba',
          edgeType: 'synergizes-with',
          evidenceTier: 'Moderate',
        }),
      ],
    };

    render(
      <CompoundRelationshipPanel
        graph={graph}
        compound={{ canonicalName: 'Creatine', slug: 'creatine' }}
      />
    );
    expect(screen.getByText('Beta-Alanine')).toBeInTheDocument();
    expect(screen.getByText(/Synergizes With/i)).toBeInTheDocument();
    expect(screen.getByText(/Moderate/i)).toBeInTheDocument();
  });

  it('renders edges where the current compound is the "to" endpoint (alias match)', () => {
    const graph: CompoundGraph = {
      ...baseGraph,
      nodes: [
        makeNode({ nodeId: 'n:bpc', label: 'BPC-157' }),
        makeNode({
          nodeId: 'n:cre',
          label: 'Creatine Monohydrate', // not the canonical name
          aliases: ['Creatine'],          // matched via alias
        }),
      ],
      edges: [
        makeEdge({
          edgeId: 'edge:bpc-pairs-cre',
          from: 'n:bpc',
          to: 'n:cre',
          edgeType: 'pairs-with',
        }),
      ],
    };

    render(
      <CompoundRelationshipPanel
        graph={graph}
        compound={{
          canonicalName: 'Creatine',
          slug: 'creatine',
          aliases: ['Creatine Monohydrate'],
        }}
      />
    );
    expect(screen.getByText('BPC-157')).toBeInTheDocument();
    expect(screen.getByText(/Pairs With/i)).toBeInTheDocument();
  });

  it('matches via slug fallback when neither canonical name nor aliases match', () => {
    const graph: CompoundGraph = {
      ...baseGraph,
      nodes: [
        // node label differs from canonical name; aliases don't match either; only the id slug matches
        makeNode({ nodeId: 'compound:creatine', label: 'creatine (rebranded)' }),
        makeNode({ nodeId: 'n:ba', label: 'Beta-Alanine' }),
      ],
      edges: [
        makeEdge({
          edgeId: 'edge:cre-comp-ba',
          from: 'compound:creatine',
          to: 'n:ba',
          edgeType: 'complements',
        }),
      ],
    };

    render(
      <CompoundRelationshipPanel
        graph={graph}
        compound={{ canonicalName: 'Creatine', slug: 'creatine' }}
      />
    );
    expect(screen.getByText('Beta-Alanine')).toBeInTheDocument();
    expect(screen.getByText(/Complements/i)).toBeInTheDocument();
  });

  it('filters out taxonomic edges and unrelated edges', () => {
    const graph: CompoundGraph = {
      ...baseGraph,
      nodes: [
        makeNode({ nodeId: 'n:cre', label: 'Creatine' }),
        makeNode({ nodeId: 'n:ba', label: 'Beta-Alanine' }),
        makeNode({ nodeId: 'n:bpc', label: 'BPC-157' }),
        makeNode({ nodeId: 'n:sem', label: 'Semaglutide' }),
        makeNode({
          nodeId: 'category:performance', label: 'Performance Supplement', nodeType: 'category',
        }),
      ],
      edges: [
        // Belongs to category — taxonomic, must be filtered
        makeEdge({
          edgeId: 'edge:taxonomic',
          from: 'n:cre',
          to: 'category:performance',
          edgeType: 'belongs-to-category',
        }),
        // Relationship edge between two OTHER compounds — must be filtered
        makeEdge({
          edgeId: 'edge:unrelated',
          from: 'n:bpc',
          to: 'n:sem',
          edgeType: 'pairs-with',
        }),
        // Relationship edge that DOES involve Creatine — kept
        makeEdge({
          edgeId: 'edge:kept',
          from: 'n:cre',
          to: 'n:ba',
          edgeType: 'synergizes-with',
        }),
      ],
    };

    render(
      <CompoundRelationshipPanel
        graph={graph}
        compound={{ canonicalName: 'Creatine', slug: 'creatine' }}
      />
    );

    expect(screen.getByText('Beta-Alanine')).toBeInTheDocument();
    expect(screen.queryByText('BPC-157')).not.toBeInTheDocument();
    expect(screen.queryByText('Semaglutide')).not.toBeInTheDocument();
    expect(screen.queryByText('Performance Supplement')).not.toBeInTheDocument();
  });

  it('renders the community-signal badge and the review-required badge when applicable', () => {
    const graph: CompoundGraph = {
      ...baseGraph,
      nodes: [
        makeNode({ nodeId: 'n:cre', label: 'Creatine' }),
        makeNode({ nodeId: 'n:ba', label: 'Beta-Alanine' }),
      ],
      edges: [
        makeEdge({
          edgeId: 'edge:cre-comm-ba',
          from: 'n:cre',
          to: 'n:ba',
          edgeType: 'has-community-signal',
          needsReview: true,
          evidenceTier: 'Anecdotal',
          communitySignal: {
            present: true,
            signalStrength: 'recurring',
            signalDirection: 'positive',
            signalUse: 'stack-pattern',
            canonicalTruthStatus: 'unsupported',
            notes: 'Frequent self-reports.',
          },
        }),
      ],
    };
    render(
      <CompoundRelationshipPanel
        graph={graph}
        compound={{ canonicalName: 'Creatine', slug: 'creatine' }}
      />
    );

    expect(screen.getAllByText(/Community signal/i).length).toBeGreaterThanOrEqual(1);
    expect(screen.getByText(/Recurring/i)).toBeInTheDocument();
    expect(screen.getByText(/Review Required/i)).toBeInTheDocument();
    expect(screen.getByText(/Anecdotal/i)).toBeInTheDocument();
  });

  it('renders the "Asserted → effective" hint when assertedRelationshipType differs from edgeType', () => {
    const graph: CompoundGraph = {
      ...baseGraph,
      nodes: [
        makeNode({ nodeId: 'n:cre', label: 'Creatine' }),
        makeNode({ nodeId: 'n:ba', label: 'Beta-Alanine' }),
      ],
      edges: [
        makeEdge({
          edgeId: 'edge:asserted',
          from: 'n:cre',
          to: 'n:ba',
          edgeType: 'has-community-signal',
          assertedRelationshipType: 'synergizes-with',
        }),
      ],
    };
    render(
      <CompoundRelationshipPanel
        graph={graph}
        compound={{ canonicalName: 'Creatine', slug: 'creatine' }}
      />
    );

    expect(screen.getByText(/Asserted:/i)).toBeInTheDocument();
    expect(screen.getByText(/effective:/i)).toBeInTheDocument();
    // Both forms title-cased should appear (one in the asserted hint, one as the badge)
    expect(screen.getAllByText(/Synergizes With/i).length).toBeGreaterThanOrEqual(1);
    expect(screen.getAllByText(/Has Community Signal/i).length).toBeGreaterThanOrEqual(1);
  });

  it('does not render the "Asserted → effective" hint when assertedRelationshipType matches edgeType', () => {
    const graph: CompoundGraph = {
      ...baseGraph,
      nodes: [
        makeNode({ nodeId: 'n:cre', label: 'Creatine' }),
        makeNode({ nodeId: 'n:ba', label: 'Beta-Alanine' }),
      ],
      edges: [
        makeEdge({
          edgeId: 'edge:same',
          from: 'n:cre',
          to: 'n:ba',
          edgeType: 'synergizes-with',
          assertedRelationshipType: 'synergizes-with',
        }),
      ],
    };
    render(
      <CompoundRelationshipPanel
        graph={graph}
        compound={{ canonicalName: 'Creatine', slug: 'creatine' }}
      />
    );
    expect(screen.queryByText(/Asserted:/i)).not.toBeInTheDocument();
  });

  it('renders unresolved review findings that reference this compound', () => {
    const finding: CompoundGraphReviewFinding = {
      findingId: 'finding-1',
      findingType: 'popular-stack-insufficient-evidence',
      severity: 'moderate',
      compoundRefs: ['Creatine'],
      edgeRefs: ['edge:any'],
      summary: 'Recurring community claim about creatine lacks authoritative support.',
      recommendedAction: 'Flag for human review.',
      needsHumanReview: true,
    };
    const graph: CompoundGraph = {
      ...baseGraph,
      nodes: [
        makeNode({ nodeId: 'n:cre', label: 'Creatine' }),
        makeNode({ nodeId: 'n:ba', label: 'Beta-Alanine' }),
      ],
      edges: [
        makeEdge({
          edgeId: 'edge:any',
          from: 'n:cre',
          to: 'n:ba',
          edgeType: 'pairs-with',
        }),
      ],
      reviewFindings: [finding],
    };
    render(
      <CompoundRelationshipPanel
        graph={graph}
        compound={{ canonicalName: 'Creatine', slug: 'creatine' }}
      />
    );

    expect(screen.getByText(/Review Findings/i)).toBeInTheDocument();
    expect(screen.getByText(/Recurring community claim about creatine/i)).toBeInTheDocument();
    expect(screen.getByText(/Flag for human review/i)).toBeInTheDocument();
    expect(screen.getByText(/Moderate/i)).toBeInTheDocument();
  });

  it('only renders the counterpart as a link when its slug is in knownSlugs', () => {
    const graph: CompoundGraph = {
      ...baseGraph,
      nodes: [
        makeNode({ nodeId: 'compound:creatine', label: 'Creatine' }),
        makeNode({ nodeId: 'compound:beta-alanine', label: 'Beta-Alanine' }),
        makeNode({ nodeId: 'compound:unknown', label: 'Mystery Compound' }),
      ],
      edges: [
        makeEdge({
          edgeId: 'edge:linked',
          from: 'compound:creatine',
          to: 'compound:beta-alanine',
          edgeType: 'synergizes-with',
        }),
        makeEdge({
          edgeId: 'edge:unlinked',
          from: 'compound:creatine',
          to: 'compound:unknown',
          edgeType: 'pairs-with',
        }),
      ],
    };

    render(
      <CompoundRelationshipPanel
        graph={graph}
        compound={{ canonicalName: 'Creatine', slug: 'creatine' }}
        knownSlugs={new Set(['beta-alanine'])}
      />
    );

    const linked = screen.getByRole('link', { name: 'Beta-Alanine' });
    expect(linked).toHaveAttribute('href', '/admin/research/compounds/beta-alanine');

    // Mystery Compound rendered but NOT as a link
    expect(screen.queryByRole('link', { name: 'Mystery Compound' })).not.toBeInTheDocument();
    expect(screen.getByText('Mystery Compound')).toBeInTheDocument();
  });
});
