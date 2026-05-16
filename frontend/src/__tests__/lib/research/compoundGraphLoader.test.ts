import { fetchCompoundGraph } from '@/lib/research/loader';
import type { CompoundGraph } from '@/lib/research/types';

describe('fetchCompoundGraph', () => {
  const originalFetch = globalThis.fetch;

  afterEach(() => {
    globalThis.fetch = originalFetch;
    vi.restoreAllMocks();
  });

  it('normalizes PascalCase KnowledgeWorker artifact JSON into camelCase CompoundGraph', async () => {
    const pascalPayload = {
      GraphVersion: '1.0.0',
      GeneratedAtUtc: '2026-05-05T09:00:00Z',
      Counts: {
        Nodes: 2,
        Edges: 1,
        ReviewRequiredEdges: 1,
        CommunitySignalEdges: 1,
        ConflictEdges: 0,
      },
      Nodes: [
        {
          NodeId: 'compound:creatine',
          NodeType: 'compound',
          Label: 'Creatine',
          Aliases: ['Creatine Monohydrate'],
          Metadata: { Slug: 'creatine' },
        },
        {
          NodeId: 'compound:beta-alanine',
          NodeType: 'compound',
          Label: 'Beta-Alanine',
          Aliases: [],
          Metadata: { Slug: 'beta-alanine' },
        },
      ],
      Edges: [
        {
          EdgeId: 'edge:creatine-pairs-beta-alanine',
          From: 'compound:creatine',
          To: 'compound:beta-alanine',
          EdgeType: 'has-community-signal',
          RelationshipType: 'pairs-with',
          AssertedRelationshipType: 'synergizes-with',
          EffectDomain: 'exercise-performance',
          EvidenceTier: 'Moderate',
          Confidence: 'moderate',
          SourceRefs: ['src-1'],
          ClaimRefs: ['claim-1'],
          ReviewFlags: ['community-only'],
          NeedsReview: true,
          CommunitySignal: {
            Present: true,
            SignalStrength: 'recurring',
            SignalDirection: 'positive',
            SignalUse: 'stack-pattern',
            CanonicalTruthStatus: 'partially-supported',
            Notes: 'Frequent self-reports.',
          },
          SourceAuthorityMix: { AuthorityTiers: ['B', 'D'] },
        },
      ],
      ReviewFindings: [
        {
          FindingId: 'finding-1',
          FindingType: 'popular-stack-insufficient-evidence',
          Severity: 'moderate',
          CompoundRefs: ['compound:creatine'],
          EdgeRefs: ['edge:creatine-pairs-beta-alanine'],
          Summary: 'A summary.',
          RecommendedAction: 'Review it.',
          NeedsHumanReview: true,
        },
      ],
    };

    const fetchMock = vi.fn().mockResolvedValue(
      new Response(JSON.stringify(pascalPayload), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      })
    );
    globalThis.fetch = fetchMock as unknown as typeof fetch;

    const result: CompoundGraph = await fetchCompoundGraph('test-token');

    expect(fetchMock).toHaveBeenCalledTimes(1);
    const [url, init] = fetchMock.mock.calls[0];
    expect(String(url)).toContain('artifact=compound-graph');
    expect((init as RequestInit | undefined)?.headers).toEqual({
      Authorization: 'Bearer test-token',
    });

    expect(result.graphVersion).toBe('1.0.0');
    expect(result.generatedAtUtc).toBe('2026-05-05T09:00:00Z');
    expect(result.counts.nodes).toBe(2);
    expect(result.counts.reviewRequiredEdges).toBe(1);
    expect(result.counts.communitySignalEdges).toBe(1);
    expect(result.counts.conflictEdges).toBe(0);

    expect(result.nodes).toHaveLength(2);
    expect(result.nodes[0].nodeId).toBe('compound:creatine');
    expect(result.nodes[0].nodeType).toBe('compound');
    expect(result.nodes[0].aliases).toEqual(['Creatine Monohydrate']);
    expect(result.nodes[0].metadata).toEqual({ slug: 'creatine' });

    expect(result.edges).toHaveLength(1);
    const edge = result.edges[0];
    expect(edge.edgeId).toBe('edge:creatine-pairs-beta-alanine');
    expect(edge.from).toBe('compound:creatine');
    expect(edge.to).toBe('compound:beta-alanine');
    expect(edge.edgeType).toBe('has-community-signal');
    expect(edge.assertedRelationshipType).toBe('synergizes-with');
    expect(edge.needsReview).toBe(true);
    expect(edge.sourceAuthorityMix.authorityTiers).toEqual(['B', 'D']);
    expect(edge.communitySignal?.signalStrength).toBe('recurring');
    expect(edge.communitySignal?.canonicalTruthStatus).toBe('partially-supported');

    expect(result.reviewFindings).toHaveLength(1);
    expect(result.reviewFindings[0].findingId).toBe('finding-1');
    expect(result.reviewFindings[0].needsHumanReview).toBe(true);
    expect(result.reviewFindings[0].compoundRefs).toEqual(['compound:creatine']);
  });

  it('throws when the artifact endpoint returns a non-OK response', async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      new Response('forbidden', { status: 403 })
    );
    globalThis.fetch = fetchMock as unknown as typeof fetch;

    await expect(fetchCompoundGraph('bad-token')).rejects.toThrow(
      /Failed to fetch compound-graph: 403/
    );
  });

  it('propagates network errors from fetch', async () => {
    const fetchMock = vi.fn().mockRejectedValue(new Error('network down'));
    globalThis.fetch = fetchMock as unknown as typeof fetch;

    await expect(fetchCompoundGraph('any')).rejects.toThrow(/network down/);
  });
});
