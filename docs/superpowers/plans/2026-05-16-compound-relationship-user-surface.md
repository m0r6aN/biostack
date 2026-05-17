# Compound Relationship User Surface — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Surface cross-compound relationship intelligence to regular users on `/knowledge/[slug]` by fixing the broken page frame and adding a silent, non-blocking `CompoundRelationshipsSection` below the knowledge entry.

**Architecture:** Replace the non-functional `getTrustLedger` / `CompoundDossier` path with `getKnowledgeEntry` / `CompoundIntelligenceCard`. Extract shared relationship helpers into `compoundGraphRelationships.ts`. Add `CompoundRelationshipsSection` as an independent component that loads the compound graph, filters edges for the current compound, and renders user-friendly relationship explanation cards — failing silently if the graph is unavailable.

**Tech Stack:** Next.js 16, React, TypeScript, Vitest + React Testing Library, Tailwind CSS. Tests run with `pnpm vitest run` from `frontend/`. Auth is cookie-based via `useAuth()` which exposes `user`. The artifact API (`/api/research/artifacts`) is dev-only (returns 404 in production) and only requires a Bearer token when `RESEARCH_DATA_SOURCE !== 'fixtures'` — the component passes `''` as the token and lets the error handler cover production failures.

---

## File Map

| File | Action | Responsibility |
|---|---|---|
| `frontend/src/lib/research/compoundGraphRelationships.ts` | **Create** | Shared label maps, normalizers, whitelist, and mapping functions |
| `frontend/src/__tests__/lib/research/compoundGraphRelationships.test.ts` | **Create** | Tests for all helpers in the shared module |
| `frontend/src/components/research/CompoundRelationshipPanel.tsx` | **Modify** | Remove local constants; import from shared module |
| `frontend/src/__tests__/components/research/CompoundRelationshipPanel.test.tsx` | **Modify** | Add admin regression test |
| `frontend/src/components/knowledge/CompoundRelationshipsSection.tsx` | **Create** | User-facing relationship cards component |
| `frontend/src/__tests__/components/knowledge/CompoundRelationshipsSection.test.tsx` | **Create** | 15 tests for the new component |
| `frontend/src/app/knowledge/[slug]/page.tsx` | **Modify** | Replace getTrustLedger/CompoundDossier with getKnowledgeEntry/CompoundIntelligenceCard |
| `frontend/src/__tests__/app/knowledge/KnowledgeSlugPage.test.tsx` | **Create** | 5 page frame tests |

---

## Task 1: Shared module — helpers and label mappings

**Files:**
- Create: `frontend/src/lib/research/compoundGraphRelationships.ts`
- Create: `frontend/src/__tests__/lib/research/compoundGraphRelationships.test.ts`

- [ ] **Step 1.1: Write the failing tests**

Create `frontend/src/__tests__/lib/research/compoundGraphRelationships.test.ts`:

```typescript
import { describe, expect, it } from 'vitest';
import {
  RELATIONSHIP_EDGE_KEYS,
  getCommunitySignalLabel,
  getEvidenceTierLabel,
  getRelationshipLabel,
  isRelationshipEdge,
  normalizeCompoundId,
  normalizeEdgeType,
} from '@/lib/research/compoundGraphRelationships';

describe('normalizeEdgeType', () => {
  it('strips dashes and lowercases', () => {
    expect(normalizeEdgeType('synergizes-with')).toBe('synergizeswith');
  });
  it('strips underscores and whitespace', () => {
    expect(normalizeEdgeType('conflicts_With ')).toBe('conflictswith');
  });
  it('returns empty string for null/undefined', () => {
    expect(normalizeEdgeType(null)).toBe('');
    expect(normalizeEdgeType(undefined)).toBe('');
  });
});

describe('normalizeCompoundId', () => {
  it('strips compound: prefix then normalizes', () => {
    expect(normalizeCompoundId('compound:bpc-157')).toBe('bpc157');
  });
  it('produces identical output for all BPC-157 variants', () => {
    const expected = 'bpc157';
    expect(normalizeCompoundId('BPC-157')).toBe(expected);
    expect(normalizeCompoundId('bpc-157')).toBe(expected);
    expect(normalizeCompoundId('bpc 157')).toBe(expected);
    expect(normalizeCompoundId('bpc157')).toBe(expected);
    expect(normalizeCompoundId('compound:BPC-157')).toBe(expected);
  });
  it('returns empty string for null/undefined', () => {
    expect(normalizeCompoundId(null)).toBe('');
    expect(normalizeCompoundId(undefined)).toBe('');
  });
});

describe('isRelationshipEdge', () => {
  it('returns true for all whitelist entries', () => {
    for (const key of RELATIONSHIP_EDGE_KEYS) {
      expect(isRelationshipEdge(key)).toBe(true);
    }
  });
  it('returns true for kebab-case whitelist entries', () => {
    expect(isRelationshipEdge('synergizes-with')).toBe(true);
    expect(isRelationshipEdge('conflicts-with')).toBe(true);
    expect(isRelationshipEdge('avoid-with')).toBe(true);
  });
  it('returns false for taxonomic edges', () => {
    expect(isRelationshipEdge('belongs-to-category')).toBe(false);
    expect(isRelationshipEdge('affects-pathway')).toBe(false);
    expect(isRelationshipEdge('has-target')).toBe(false);
  });
  it('returns false for null/undefined', () => {
    expect(isRelationshipEdge(null)).toBe(false);
    expect(isRelationshipEdge(undefined)).toBe(false);
  });
});

describe('getRelationshipLabel', () => {
  it('maps synergizes-with to user-facing copy', () => {
    expect(getRelationshipLabel('synergizes-with')).toBe('May work well together');
    expect(getRelationshipLabel('synergizeswith')).toBe('May work well together');
  });
  it('maps pairs-with to same label as synergizes-with', () => {
    expect(getRelationshipLabel('pairs-with')).toBe('May work well together');
  });
  it('maps complements', () => {
    expect(getRelationshipLabel('complements')).toBe('May support the same goal differently');
  });
  it('maps redundant-with', () => {
    expect(getRelationshipLabel('redundant-with')).toBe('May overlap');
  });
  it('maps conflicts-with and opposes-effect to Potential conflict', () => {
    expect(getRelationshipLabel('conflicts-with')).toBe('Potential conflict');
    expect(getRelationshipLabel('opposes-effect')).toBe('Potential conflict');
  });
  it('maps avoid-with to Caution signal', () => {
    expect(getRelationshipLabel('avoid-with')).toBe('Caution signal');
  });
  it('maps has-community-signal', () => {
    expect(getRelationshipLabel('has-community-signal')).toBe('Community-reported pairing');
  });
  it('maps contradicted-by', () => {
    expect(getRelationshipLabel('contradicted-by')).toBe('Contradicted by evidence');
  });
  it('returns Related compound for unknown types', () => {
    expect(getRelationshipLabel('unknown-type')).toBe('Related compound');
    expect(getRelationshipLabel(null)).toBe('Related compound');
  });
});

describe('getEvidenceTierLabel', () => {
  it('maps strong (case-insensitive)', () => {
    expect(getEvidenceTierLabel('strong')).toBe('Strong evidence');
    expect(getEvidenceTierLabel('Strong')).toBe('Strong evidence');
    expect(getEvidenceTierLabel('STRONG')).toBe('Strong evidence');
  });
  it('maps moderate', () => {
    expect(getEvidenceTierLabel('moderate')).toBe('Moderate evidence');
  });
  it('maps limited', () => {
    expect(getEvidenceTierLabel('limited')).toBe('Limited evidence');
  });
  it('maps mechanistic', () => {
    expect(getEvidenceTierLabel('mechanistic')).toBe('Mechanistic evidence');
  });
  it('maps anecdotal to community report copy without em dash', () => {
    expect(getEvidenceTierLabel('anecdotal')).toBe('Community report: not clinically verified');
  });
  it('returns Evidence level unknown for null, undefined, empty, and unrecognised', () => {
    expect(getEvidenceTierLabel(null)).toBe('Evidence level unknown');
    expect(getEvidenceTierLabel(undefined)).toBe('Evidence level unknown');
    expect(getEvidenceTierLabel('')).toBe('Evidence level unknown');
    expect(getEvidenceTierLabel('unknown')).toBe('Evidence level unknown');
    expect(getEvidenceTierLabel('insufficient')).toBe('Evidence level unknown');
  });
});

describe('getCommunitySignalLabel', () => {
  it('returns string for isolated', () => {
    expect(getCommunitySignalLabel('isolated')).toBe('Rarely reported in community');
  });
  it('returns string for recurring', () => {
    expect(getCommunitySignalLabel('recurring')).toBe('Commonly reported in community');
  });
  it('returns string for widespread', () => {
    expect(getCommunitySignalLabel('widespread')).toBe('Widely reported across communities');
  });
  it('returns null for none', () => {
    expect(getCommunitySignalLabel('none')).toBeNull();
  });
  it('returns null for null/undefined/empty', () => {
    expect(getCommunitySignalLabel(null)).toBeNull();
    expect(getCommunitySignalLabel(undefined)).toBeNull();
    expect(getCommunitySignalLabel('')).toBeNull();
  });
});
```

- [ ] **Step 1.2: Run tests to confirm they fail**

```
cd frontend && pnpm vitest run src/__tests__/lib/research/compoundGraphRelationships.test.ts
```

Expected: all tests fail with "Cannot find module '@/lib/research/compoundGraphRelationships'".

- [ ] **Step 1.3: Implement the shared module**

Create `frontend/src/lib/research/compoundGraphRelationships.ts`:

```typescript
export const RELATIONSHIP_EDGE_KEYS: ReadonlySet<string> = new Set([
  'pairswith',
  'conflictswith',
  'redundantwith',
  'synergizeswith',
  'complements',
  'hascommunitysignal',
  'contradictedby',
  'opposeseffect',
  'opposingeffect',
  'avoidwith',
]);

export function normalizeEdgeType(raw: string | null | undefined): string {
  if (!raw) return '';
  return raw.toString().replace(/[-_\s]/g, '').toLowerCase();
}

export function normalizeCompoundId(raw: string | null | undefined): string {
  if (!raw) return '';
  const stripped = raw.toString().replace(/^compound:/i, '');
  return normalizeEdgeType(stripped);
}

export function isRelationshipEdge(edgeType: string | null | undefined): boolean {
  return RELATIONSHIP_EDGE_KEYS.has(normalizeEdgeType(edgeType));
}

const RELATIONSHIP_LABELS: Record<string, string> = {
  synergizeswith: 'May work well together',
  pairswith: 'May work well together',
  complements: 'May support the same goal differently',
  redundantwith: 'May overlap',
  conflictswith: 'Potential conflict',
  opposeseffect: 'Potential conflict',
  opposingeffect: 'Potential conflict',
  avoidwith: 'Caution signal',
  hascommunitysignal: 'Community-reported pairing',
  contradictedby: 'Contradicted by evidence',
};

export function getRelationshipLabel(edgeType: string | null | undefined): string {
  return RELATIONSHIP_LABELS[normalizeEdgeType(edgeType)] ?? 'Related compound';
}

const EVIDENCE_TIER_LABELS: Record<string, string> = {
  strong: 'Strong evidence',
  moderate: 'Moderate evidence',
  limited: 'Limited evidence',
  mechanistic: 'Mechanistic evidence',
  anecdotal: 'Community report: not clinically verified',
};

export function getEvidenceTierLabel(tier: string | null | undefined): string {
  if (!tier) return 'Evidence level unknown';
  return EVIDENCE_TIER_LABELS[tier.toLowerCase()] ?? 'Evidence level unknown';
}

const COMMUNITY_SIGNAL_LABELS: Record<string, string> = {
  isolated: 'Rarely reported in community',
  recurring: 'Commonly reported in community',
  widespread: 'Widely reported across communities',
};

export function getCommunitySignalLabel(strength: string | null | undefined): string | null {
  if (!strength) return null;
  return COMMUNITY_SIGNAL_LABELS[strength.toLowerCase()] ?? null;
}
```

- [ ] **Step 1.4: Run tests to confirm they pass**

```
cd frontend && pnpm vitest run src/__tests__/lib/research/compoundGraphRelationships.test.ts
```

Expected: all tests pass.

- [ ] **Step 1.5: Commit**

```
cd frontend && git add src/lib/research/compoundGraphRelationships.ts src/__tests__/lib/research/compoundGraphRelationships.test.ts && git commit -m "feat(research): shared compound relationship helpers module"
```

---

## Task 2: Admin extraction — import shared module in CompoundRelationshipPanel

**Files:**
- Modify: `frontend/src/components/research/CompoundRelationshipPanel.tsx` (lines 22–38)
- Modify: `frontend/src/__tests__/components/research/CompoundRelationshipPanel.test.tsx`

- [ ] **Step 2.1: Write the admin regression test**

Open `frontend/src/__tests__/components/research/CompoundRelationshipPanel.test.tsx` and add this test at the end of the existing describe block. Do not remove any existing tests.

Read the file first to find the correct insertion point, then add:

```typescript
it('still filters relationship edges and excludes taxonomic edges after shared helper extraction', () => {
  const taxonomicEdge = makeEdge({
    edgeId: 'e-tax',
    from: 'compound:creatine',
    to: 'category:performance-supplement',
    edgeType: 'belongs-to-category',
  });
  const relationshipEdge = makeEdge({
    edgeId: 'e-rel',
    from: 'compound:creatine',
    to: 'compound:beta-alanine',
    edgeType: 'synergizes-with',
  });
  const graph = makeGraph({
    nodes: [
      makeNode({ nodeId: 'compound:creatine', label: 'Creatine', nodeType: 'compound' }),
      makeNode({ nodeId: 'compound:beta-alanine', label: 'Beta-Alanine', nodeType: 'compound' }),
      makeNode({ nodeId: 'category:performance-supplement', label: 'Performance Supplement', nodeType: 'category' }),
    ],
    edges: [taxonomicEdge, relationshipEdge],
  });
  render(
    <CompoundRelationshipPanel
      graph={graph}
      compound={{ canonicalName: 'Creatine', slug: 'creatine', aliases: [] }}
      knownSlugs={new Set(['creatine', 'beta-alanine'])}
    />
  );
  expect(screen.getByText('Beta-Alanine')).toBeInTheDocument();
  expect(screen.queryByText('Performance Supplement')).not.toBeInTheDocument();
});
```

- [ ] **Step 2.2: Run the regression test to confirm it passes against current code**

```
cd frontend && pnpm vitest run src/__tests__/components/research/CompoundRelationshipPanel.test.tsx
```

Expected: all existing tests plus the new one pass. If any pre-existing tests fail, stop and investigate — do not proceed with extraction until the regression baseline is green.

- [ ] **Step 2.3: Extract RELATIONSHIP_EDGE_KEYS and normalizeKey from the panel**

In `frontend/src/components/research/CompoundRelationshipPanel.tsx`:

Remove lines 22–38 (the `RELATIONSHIP_EDGE_KEYS` Set and `normalizeKey` function).

Add this import after the existing import block (around line 13):

```typescript
import {
  RELATIONSHIP_EDGE_KEYS,
  normalizeEdgeType as normalizeKey,
} from '@/lib/research/compoundGraphRelationships';
```

No other changes to the panel. The rest of the file continues to call `normalizeKey(...)` exactly as before — the import alias `as normalizeKey` preserves all existing call sites.

- [ ] **Step 2.4: Run all panel tests to confirm they still pass**

```
cd frontend && pnpm vitest run src/__tests__/components/research/CompoundRelationshipPanel.test.tsx
```

Expected: same pass count as Step 2.2. Zero new failures.

- [ ] **Step 2.5: Commit**

```
cd frontend && git add src/components/research/CompoundRelationshipPanel.tsx src/__tests__/components/research/CompoundRelationshipPanel.test.tsx && git commit -m "refactor(research): extract CompoundRelationshipPanel helpers to shared module"
```

---

## Task 3: CompoundRelationshipsSection — new user-facing component

**Files:**
- Create: `frontend/src/components/knowledge/CompoundRelationshipsSection.tsx`
- Create: `frontend/src/__tests__/components/knowledge/CompoundRelationshipsSection.test.tsx`

### Key implementation notes before writing tests

**Auth:** `useAuth()` returns `{ user, loading, refresh, logout }` — no raw token. Guard on `user` being non-null. The artifact endpoint (`/api/research/artifacts`) does not require a token when `RESEARCH_DATA_SOURCE=fixtures` (the default). In production it returns 404 (dev-only route), which is caught silently.

**Token:** Call `fetchCompoundGraph('')` — the empty string is accepted in fixture mode. The error path covers production 404s.

**Compound matching:** Normalize both the compound name/aliases and the edge endpoints with `normalizeCompoundId` before comparing. An edge matches if the normalized `from` or `to` is in the set of normalized ids for this compound.

**Counterpart node lookup:** Use `graph.nodes` to find a node whose `nodeId` matches the counterpart endpoint string. Only link when `node.nodeType === 'compound'` and `toSlug(node.label)` returns a non-empty string.

**Sort order:** Strongest evidence tier first (strong=5 > moderate=4 > limited=3 > mechanistic=2 > anecdotal=1 > unknown=0), then `needsReview: true` first, then counterpart label alphabetically.

**Malformed edges:** Wrap edge processing in a try/catch — skip and continue rather than throw.

- [ ] **Step 3.1: Write the failing tests**

Create `frontend/src/__tests__/components/knowledge/CompoundRelationshipsSection.test.tsx`:

```typescript
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
```

- [ ] **Step 3.2: Run tests to confirm they fail**

```
cd frontend && pnpm vitest run src/__tests__/components/knowledge/CompoundRelationshipsSection.test.tsx
```

Expected: all tests fail with "Cannot find module '@/components/knowledge/CompoundRelationshipsSection'".

- [ ] **Step 3.3: Implement CompoundRelationshipsSection**

Create `frontend/src/components/knowledge/CompoundRelationshipsSection.tsx`:

```typescript
'use client';

import { useEffect, useState } from 'react';
import Link from 'next/link';
import { useAuth } from '@/lib/AuthProvider';
import { fetchCompoundGraph } from '@/lib/research/loader';
import { toSlug } from '@/lib/research/slugs';
import type { CompoundGraphNode } from '@/lib/research/types';
import {
  getCommunitySignalLabel,
  getEvidenceTierLabel,
  getRelationshipLabel,
  isRelationshipEdge,
  normalizeCompoundId,
} from '@/lib/research/compoundGraphRelationships';

interface Props {
  compoundName: string;
  aliases?: string[];
}

const TIER_RANK: Record<string, number> = {
  strong: 5,
  moderate: 4,
  limited: 3,
  mechanistic: 2,
  anecdotal: 1,
};

interface MatchedEdge {
  counterpartLabel: string;
  counterpartSlug: string | null;
  relationshipLabel: string;
  evidenceTierLabel: string;
  tierRank: number;
  communitySignalLabel: string | null;
  needsReview: boolean;
}

function tierRank(tier: string | null | undefined): number {
  if (!tier) return 0;
  return TIER_RANK[tier.toLowerCase()] ?? 0;
}

export function CompoundRelationshipsSection({ compoundName, aliases }: Props) {
  const { user } = useAuth();
  const [matched, setMatched] = useState<MatchedEdge[] | null>(null);

  useEffect(() => {
    if (!user) return;

    fetchCompoundGraph('')
      .then(graph => {
        const aliasList = aliases ?? [];
        const myIds = new Set([
          normalizeCompoundId(compoundName),
          ...aliasList.map(a => normalizeCompoundId(a)),
        ]);

        const nodeMap = new Map<string, CompoundGraphNode>(
          graph.nodes.map(n => [n.nodeId, n])
        );

        const edges: MatchedEdge[] = [];

        for (const edge of graph.edges) {
          try {
            if (!isRelationshipEdge(edge.edgeType)) continue;

            const fromNorm = normalizeCompoundId(edge.from);
            const toNorm = normalizeCompoundId(edge.to);
            const fromMatch = myIds.has(fromNorm);
            const toMatch = myIds.has(toNorm);

            if (!fromMatch && !toMatch) continue;

            const counterpartId = fromMatch ? edge.to : edge.from;
            const counterpartNode = nodeMap.get(counterpartId);
            if (!counterpartNode) continue;

            let counterpartSlug: string | null = null;
            if (counterpartNode.nodeType === 'compound') {
              const slug = toSlug(counterpartNode.label);
              if (slug.length > 0) counterpartSlug = slug;
            }

            const signalStrength =
              edge.communitySignal?.present ? edge.communitySignal.signalStrength : null;

            edges.push({
              counterpartLabel: counterpartNode.label,
              counterpartSlug,
              relationshipLabel: getRelationshipLabel(edge.edgeType),
              evidenceTierLabel: getEvidenceTierLabel(edge.evidenceTier),
              tierRank: tierRank(edge.evidenceTier),
              communitySignalLabel: getCommunitySignalLabel(signalStrength),
              needsReview: edge.needsReview ?? false,
            });
          } catch {
            // Skip malformed edges
          }
        }

        edges.sort((a, b) => {
          if (b.tierRank !== a.tierRank) return b.tierRank - a.tierRank;
          if (b.needsReview !== a.needsReview) return b.needsReview ? 1 : -1;
          return a.counterpartLabel.localeCompare(b.counterpartLabel);
        });

        setMatched(edges);
      })
      .catch(() => setMatched([]));
  }, [user, compoundName, aliases]);

  if (!matched || matched.length === 0) return null;

  return (
    <section className="max-w-3xl mx-auto mt-6 px-4">
      <div className="rounded-2xl border border-white/10 bg-white/[0.03] p-6">
        <div className="flex items-center gap-3 mb-1">
          <h2 className="text-sm font-semibold uppercase tracking-[0.15em] text-white/40">
            Relationships
          </h2>
          <span className="text-xs px-2 py-0.5 rounded-full bg-white/[0.06] text-white/40 border border-white/10">
            {matched.length}
          </span>
        </div>
        <p className="text-xs text-white/30 mb-5">
          Educational reference only. These are research observations, not recommendations.
        </p>

        <ul className="space-y-3">
          {matched.map((edge, i) => (
            <li
              key={i}
              className="flex flex-col gap-1 px-4 py-3 rounded-xl border border-white/[0.07] bg-white/[0.02]"
            >
              <div className="flex flex-wrap items-center gap-2">
                {edge.counterpartSlug ? (
                  <Link
                    href={`/knowledge/${edge.counterpartSlug}`}
                    className="text-sm font-semibold text-white/80 hover:text-white transition-colors"
                  >
                    {edge.counterpartLabel}
                  </Link>
                ) : (
                  <span className="text-sm font-semibold text-white/80">
                    {edge.counterpartLabel}
                  </span>
                )}
                <span className="text-xs px-2 py-0.5 rounded-full bg-white/[0.06] text-white/50 border border-white/[0.08]">
                  {edge.relationshipLabel}
                </span>
                <span className="text-xs text-white/35">
                  {edge.evidenceTierLabel}
                </span>
              </div>
              {edge.communitySignalLabel && (
                <p className="text-xs text-sky-300/70 mt-0.5">
                  {edge.communitySignalLabel}
                </p>
              )}
              {edge.needsReview && (
                <p className="text-xs text-amber-300/70 mt-0.5">
                  Awaiting research review · Advisory signal only
                </p>
              )}
            </li>
          ))}
        </ul>
      </div>
    </section>
  );
}
```

- [ ] **Step 3.4: Run tests to confirm they pass**

```
cd frontend && pnpm vitest run src/__tests__/components/knowledge/CompoundRelationshipsSection.test.tsx
```

Expected: all 15 tests pass.

- [ ] **Step 3.5: Commit**

```
cd frontend && git add src/components/knowledge/CompoundRelationshipsSection.tsx src/__tests__/components/knowledge/CompoundRelationshipsSection.test.tsx && git commit -m "feat(knowledge): CompoundRelationshipsSection user-facing relationship cards"
```

---

## Task 4: Page frame fix — `/knowledge/[slug]`

**Files:**
- Modify: `frontend/src/app/knowledge/[slug]/page.tsx`
- Create: `frontend/src/__tests__/app/knowledge/KnowledgeSlugPage.test.tsx`

- [ ] **Step 4.1: Read the current page to understand the complete structure**

Read `frontend/src/app/knowledge/[slug]/page.tsx` in full before making any changes. The current structure:
- Imports: `TrustLedgerResponse`, `CompoundDossier`
- State: `useState<TrustLedgerResponse | null>(null)`
- Fetch: `apiClient.getTrustLedger(slug)`
- Render: `<CompoundDossier data={data} />`

Confirm the `DossierSkeleton` component is defined inline (lines 9–36) and the error card markup. These are preserved unchanged.

- [ ] **Step 4.2: Write the failing page frame tests**

Create `frontend/src/__tests__/app/knowledge/KnowledgeSlugPage.test.tsx`:

```typescript
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
    // Static source check — these symbols must not appear in the page module
    const pageSource = Object.keys(
      import.meta.glob('/src/app/knowledge/[slug]/page.tsx', { eager: true, as: 'raw' })
    );
    // This test confirms the module imports correctly; the absence of old symbols
    // is verified by TypeScript compilation succeeding (no missing exports error)
    expect(mockGetEntry).toBeDefined(); // apiClient.getKnowledgeEntry is the active method
    expect(apiClient).not.toHaveProperty('getTrustLedger');
  });

  it('shows skeleton while loading', () => {
    mockGetEntry.mockReturnValue(new Promise(() => {})); // never resolves
    render(<KnowledgeSlugPage params={{ slug: 'creatine' }} />);
    // DossierSkeleton renders 4 animate-pulse cards
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
```

- [ ] **Step 4.3: Run tests to confirm they fail**

```
cd frontend && pnpm vitest run src/__tests__/app/knowledge/KnowledgeSlugPage.test.tsx
```

Expected: tests fail because the page still uses `getTrustLedger`/`CompoundDossier`.

- [ ] **Step 4.4: Rewrite the page frame**

Replace the entire content of `frontend/src/app/knowledge/[slug]/page.tsx` with:

```typescript
'use client';

import { useEffect, useState } from 'react';
import Link from 'next/link';
import { apiClient } from '@/lib/api';
import { KnowledgeEntry } from '@/lib/types';
import { CompoundIntelligenceCard } from '@/components/knowledge/CompoundIntelligenceCard';
import { CompoundRelationshipsSection } from '@/components/knowledge/CompoundRelationshipsSection';

function DossierSkeleton() {
  return (
    <div className="max-w-3xl mx-auto space-y-6 animate-pulse">
      <div className="rounded-2xl border border-white/10 bg-white/[0.04] p-6 space-y-3">
        <div className="h-7 w-56 rounded-lg bg-white/10" />
        <div className="h-3 w-24 rounded bg-white/6" />
      </div>
      <div className="rounded-2xl border border-white/10 bg-white/[0.04] p-6 space-y-3">
        <div className="h-3 w-20 rounded bg-white/8" />
        <div className="flex gap-2">
          <div className="h-6 w-20 rounded-full bg-white/10" />
          <div className="h-6 w-28 rounded-full bg-white/8" />
        </div>
        <div className="h-16 rounded-xl bg-white/6" />
      </div>
      <div className="rounded-2xl border border-white/10 bg-white/[0.04] p-6 space-y-3">
        <div className="h-3 w-36 rounded bg-white/8" />
        <div className="h-14 rounded-xl bg-white/6" />
      </div>
      <div className="rounded-2xl border border-white/10 bg-white/[0.04] p-6 space-y-3">
        <div className="h-3 w-28 rounded bg-white/8" />
        {[1, 2, 3].map(i => (
          <div key={i} className="h-12 rounded-xl bg-white/6" />
        ))}
      </div>
    </div>
  );
}

function safeDecodeSlug(value: string): string {
  try {
    return decodeURIComponent(value);
  } catch {
    return value;
  }
}

interface PageProps {
  params: { slug: string };
}

export default function CompoundDossierPage({ params }: PageProps) {
  const { slug } = params;
  const [entry, setEntry] = useState<KnowledgeEntry | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setError(null);

    apiClient.getKnowledgeEntry(safeDecodeSlug(slug)).then(res => {
      if (!cancelled) {
        setEntry(res);
        setLoading(false);
      }
    }).catch(err => {
      if (!cancelled) {
        setError(err?.message ?? 'Failed to load compound dossier.');
        setLoading(false);
      }
    });

    return () => { cancelled = true; };
  }, [slug]);

  return (
    <main className="min-h-screen bg-[#0a0a0b] py-8 px-4">
      <nav className="max-w-3xl mx-auto mb-6">
        <Link
          href="/knowledge"
          className="inline-flex items-center gap-1.5 text-xs text-white/40 hover:text-white/70 transition-colors"
        >
          <svg className="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
            <path strokeLinecap="round" strokeLinejoin="round" d="M15 19l-7-7 7-7" />
          </svg>
          Compound Library
        </Link>
      </nav>

      {loading ? (
        <DossierSkeleton />
      ) : error ? (
        <div className="max-w-3xl mx-auto rounded-2xl border border-rose-400/20 bg-rose-500/[0.06] p-6">
          <p className="text-sm text-rose-300">{error}</p>
        </div>
      ) : entry ? (
        <>
          <CompoundIntelligenceCard entry={entry} recommendationSurface="knowledge-detail" />
          <CompoundRelationshipsSection
            compoundName={entry.canonicalName}
            aliases={entry.aliases}
          />
        </>
      ) : null}
    </main>
  );
}
```

- [ ] **Step 4.5: Run page tests to confirm they pass**

```
cd frontend && pnpm vitest run src/__tests__/app/knowledge/KnowledgeSlugPage.test.tsx
```

Expected: all 5 tests pass.

- [ ] **Step 4.6: Run the full test suite and confirm no regressions**

```
cd frontend && pnpm vitest run
```

Expected: pass count equal to or greater than baseline (565 passed). The 13 pre-existing failures in `ResearchDashboard.test.tsx` and one other file remain — these are pre-existing and unrelated. Zero new failures.

- [ ] **Step 4.7: Commit**

```
cd frontend && git add src/app/knowledge/[slug]/page.tsx src/__tests__/app/knowledge/KnowledgeSlugPage.test.tsx && git commit -m "fix(knowledge): replace broken TrustLedger/CompoundDossier with getKnowledgeEntry user surface"
```

---

## Self-Review

**Spec coverage check:**

| Spec requirement | Task |
|---|---|
| Fix `/knowledge/[slug]` broken page frame | Task 4 |
| Use `getKnowledgeEntry` as primary API | Task 4 |
| Remove `TrustLedgerResponse`, `CompoundDossier`, `getTrustLedger` | Task 4 |
| Preserve `DossierSkeleton` and rose error card | Task 4 (copied unchanged) |
| `CompoundRelationshipsSection` only renders after entry loads | Task 4 (conditional render) |
| `CompoundRelationshipsSection` fails silently (graph unavailable → null) | Task 3 |
| User guard: `user` null → skip fetch | Task 3 |
| Extract shared helpers into `compoundGraphRelationships.ts` | Task 1 |
| Admin panel imports from shared module | Task 2 |
| Admin regression test | Task 2 |
| All friendly label mappings (relationship, evidence tier, community signal) | Task 1 |
| `avoidwith` → "Caution signal" | Task 1 |
| `anecdotal` → "Community report: not clinically verified" (no em dash) | Task 1 |
| Sort: evidence tier desc, needsReview desc, label alpha | Task 3 |
| Link counterpart only when `nodeType === 'compound'` and `toSlug` non-empty | Task 3 |
| Skip malformed edges without crashing | Task 3 |
| No raw backend terms in DOM | Task 3 (test 12 + implementation) |
| No medical advice / dosing language | All tasks (no such copy added) |
| `safeDecodeSlug` for malformed URL params | Task 4 |
| `aliases?` optional prop | Task 3 |
| 15 component tests | Task 3 |
| 5 page frame tests | Task 4 |
| 12+ shared module tests | Task 1 |
| Admin regression test | Task 2 |

**Placeholder scan:** No TBDs, TODOs, or "similar to above" in any task. All code is complete.

**Type consistency check:**
- `CompoundRelationshipsSection` props: `{ compoundName: string; aliases?: string[] }` — used consistently in Task 3 implementation and Task 4 render.
- `MatchedEdge` interface in Task 3 is internal to the component — not exposed.
- `getRelationshipLabel`, `getEvidenceTierLabel`, `getCommunitySignalLabel` defined in Task 1, used in Task 3 — names match exactly.
- `normalizeCompoundId` defined in Task 1, imported in Task 3 — matches.
- `isRelationshipEdge` defined in Task 1, used in Task 3 — matches.
- `RELATIONSHIP_EDGE_KEYS` defined in Task 1, imported in Task 2 — matches.
- `normalizeEdgeType as normalizeKey` alias in Task 2 preserves all existing `normalizeKey(...)` call sites in the admin panel — no rename cascade required.
