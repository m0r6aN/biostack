# Compound Relationship User Surface — Design Spec
**Date:** 2026-05-16  
**PR:** 2 — User-facing compound relationship explanations  
**Status:** Approved

---

## Problem

`/knowledge/[slug]` is the user-facing compound detail page. It currently calls `apiClient.getTrustLedger(slug)`, a method that does not exist, and renders `CompoundDossier`, a research/admin artifact viewer. Regular users see an error card. The cross-compound relationship graph (delivered in commit 20db402) has no user-facing surface at all.

---

## Goal

1. Fix `/knowledge/[slug]` so it renders correctly for regular users using the working `getKnowledgeEntry` API.
2. Add `CompoundRelationshipsSection` below the knowledge entry — a silent, non-blocking enrichment that surfaces relationship intelligence in plain language.
3. Move shared relationship-graph constants into a shared module to prevent admin/user drift.

---

## Non-goals / constraints

- No backend changes.
- No `TrustLedgerResponse` or `CompoundDossier` behavior.
- No medical advice, dosing recommendations, diagnosis, or recommendation language.
- No graph visualization — relationship cards only.
- Graph load failure must never affect the knowledge entry success/error state.

---

## Architecture

Two independent data concerns on the same page:

```
/knowledge/[slug]/page.tsx
│
├── Primary contract: apiClient.getKnowledgeEntry(safeDecodeSlug(slug))
│   ├── loading  → DossierSkeleton (existing)
│   ├── error    → rose error card (existing)
│   └── success  →
│       <CompoundIntelligenceCard entry={entry} recommendationSurface="knowledge-detail" />
│       {entry loaded?
│         <CompoundRelationshipsSection
│           compoundName={entry.canonicalName}
│           aliases={entry.aliases}
│         />
│       }
│
└── Relationship enrichment: independently loads fetchCompoundGraph(token)
    ├── graph unavailable / no matches → null (silent)
    └── matches found → relationship explanation cards
```

**Invariant:** A graph load failure must not change or interfere with the entry success/error state.

---

## Shared module — `frontend/src/lib/research/compoundGraphRelationships.ts`

Extracted from `CompoundRelationshipPanel.tsx`. The admin panel imports from here after extraction — no admin behavior changes.

### Exports

```ts
/**
 * Whitelist of normalized edge type strings that represent cross-compound
 * relationships. Taxonomic edges (belongs-to-category, affects-pathway, etc.)
 * are excluded.
 */
export const RELATIONSHIP_EDGE_KEYS: ReadonlySet<string>

/**
 * Normalize an edge type string for whitelist comparison.
 * Strips hyphens, underscores, and whitespace; lowercases.
 * "synergizes-with" → "synergizeswith"
 */
export function normalizeEdgeType(raw: string | null | undefined): string

/**
 * Normalize a compound node ID or name for matching.
 * Strips "compound:" prefix, then applies normalizeEdgeType rules.
 * "compound:BPC-157" → "bpc157"
 * "BPC-157" → "bpc157"
 * "bpc157"  → "bpc157"
 */
export function normalizeCompoundId(raw: string | null | undefined): string

/**
 * Returns true if the edge type belongs to the relationship whitelist.
 */
export function isRelationshipEdge(edgeType: string | null | undefined): boolean

/**
 * Maps a normalized edge type to a user-facing relationship label.
 * Returns "Related compound" for unknown types.
 */
export function getRelationshipLabel(edgeType: string | null | undefined): string

/**
 * Maps an evidence tier string (case-insensitive) to a user-facing label.
 * Returns "Evidence level unknown" for null or unrecognised values.
 */
export function getEvidenceTierLabel(tier: string | null | undefined): string

/**
 * Maps a community signal strength string to a user-facing label.
 * Returns null for "none" or absent — caller should not render a badge.
 */
export function getCommunitySignalLabel(
  strength: string | null | undefined
): string | null
```

### Label mappings

**Relationship labels (`getRelationshipLabel`):**

| Normalized edge type | User-facing label |
|---|---|
| `synergizeswith`, `pairswith` | "May work well together" |
| `complements` | "May support the same goal differently" |
| `redundantwith` | "May overlap" |
| `conflictswith`, `opposeseffect`, `opposingeffect` | "Potential conflict" |
| `avoidwith` | "Use with caution together" |
| `hascommunitysignal` | "Community-reported pairing" |
| `contradictedby` | "Contradicted by evidence" |
| *(unknown)* | "Related compound" |

**Evidence tier labels (`getEvidenceTierLabel`, input normalized to lowercase):**

| Input | User-facing label |
|---|---|
| `strong` | "Strong evidence" |
| `moderate` | "Moderate evidence" |
| `limited` | "Limited evidence" |
| `mechanistic` | "Mechanistic evidence" |
| `anecdotal` | "Community report — not clinically verified" |
| `insufficient`, `unknown`, null, unrecognised | "Evidence level unknown" |

**Community signal labels (`getCommunitySignalLabel`):**

| Input | User-facing label |
|---|---|
| `isolated` | "Rarely reported in community" |
| `recurring` | "Commonly reported in community" |
| `widespread` | "Widely reported across communities" |
| `none`, absent, null | `null` (do not render badge) |

---

## Page frame — `/knowledge/[slug]/page.tsx`

### Removals

- `import { TrustLedgerResponse }` — deleted
- `import { CompoundDossier }` — deleted
- `useState<TrustLedgerResponse | null>` — deleted
- `apiClient.getTrustLedger(slug)` call — deleted

### Additions / replacements

```tsx
// Safe slug decode — prevents decodeURIComponent crash on malformed input
function safeDecodeSlug(value: string): string {
  try { return decodeURIComponent(value); }
  catch { return value; }
}

// State
const [entry, setEntry] = useState<KnowledgeEntry | null>(null);

// Fetch
apiClient.getKnowledgeEntry(safeDecodeSlug(slug))
  .then(res => { setEntry(res); setLoading(false); })
  .catch(err => { setError(err?.message ?? 'Failed to load compound.'); setLoading(false); });

// Success render
{entry ? (
  <>
    <CompoundIntelligenceCard entry={entry} recommendationSurface="knowledge-detail" />
    <CompoundRelationshipsSection
      compoundName={entry.canonicalName}
      aliases={entry.aliases}
    />
  </>
) : null}
```

`DossierSkeleton` and the rose error card are preserved unchanged.

---

## New component — `CompoundRelationshipsSection`

**File:** `frontend/src/components/knowledge/CompoundRelationshipsSection.tsx`

**Props:**
```ts
interface Props {
  compoundName: string;
  aliases?: string[];   // defensive; undefined treated as []
}
```

**Behaviour:**

1. On mount, read token from `useAuth()`. Call `fetchCompoundGraph(token)`.
2. On any fetch error → set internal graph state to null, render nothing.
3. Filter edges: `isRelationshipEdge(edge.edgeType)` must return true.
4. Match edges: normalize both endpoints with `normalizeCompoundId`. Compare against `normalizeCompoundId(compoundName)` and every `normalizeCompoundId(alias)` in `aliases ?? []`.
5. Extract counterpart: the endpoint that is NOT this compound. Look up its node from `graph.nodes` by `nodeId`. Confirm `counterpart.nodeType === 'compound'` before linking.
6. If zero matched edges after filtering → render nothing.
7. Sort matched edges: evidence tier strength descending (`strong` > `moderate` > `limited` > `mechanistic` > `anecdotal` > unknown), then `needsReview` descending (true first), then counterpart label alphabetically.
8. Render section.

**Evidence tier sort order (descending):**
`strong` = 5, `moderate` = 4, `limited` = 3, `mechanistic` = 2, `anecdotal` = 1, unknown = 0.

**Counterpart link rule:**
- Only link if `counterpart.nodeType === 'compound'` (exact match on the typed `CompoundGraphNodeType`).
- Derive slug using existing `toSlug(counterpart.label)` from `lib/research/slugs.ts`.
- Link target: `/knowledge/${slug}`.
- If counterpart is not a confirmed compound node, render plain text.

**Fields explicitly excluded from the DOM:**
`assertedRelationshipType`, `sourceAuthorityMix`, `canonicalTruthStatus`, `reviewFlags` array contents, `sourceRefs`, `claimRefs`, raw `edgeType` strings.

**Section layout (cards, no graph):**

```
┌─ Relationships ────────────────────────────── [N badge] ─┐
│ Educational reference only. These are research            │
│ observations, not recommendations.                        │
│                                                           │
│  [Counterpart name]  [Relationship label]  [Tier label]  │
│                      [Community signal?]                  │
│                      [Review required?]                   │
│                                                           │
│  ... one row per matched edge, sorted as above            │
└───────────────────────────────────────────────────────────┘
```

**Review required copy:** `"Awaiting research review · Advisory signal only"`

**Graceful edge degradation:** If an individual edge has unexpected/partial fields, skip that edge without throwing. The section renders the remaining valid edges.

---

## Admin extraction — `CompoundRelationshipPanel.tsx`

`RELATIONSHIP_EDGE_KEYS` and `normalizeKey` (renamed `normalizeEdgeType`) move from the panel into `compoundGraphRelationships.ts`. The panel imports from the shared module. No rendering behavior changes.

---

## Files changed

| File | Change type |
|---|---|
| `frontend/src/app/knowledge/[slug]/page.tsx` | Replace broken page frame |
| `frontend/src/components/knowledge/CompoundRelationshipsSection.tsx` | New component |
| `frontend/src/lib/research/compoundGraphRelationships.ts` | New shared module |
| `frontend/src/components/research/CompoundRelationshipPanel.tsx` | Import from shared module (extraction only) |

---

## Test plan

### Shared module tests (`__tests__/lib/research/compoundGraphRelationships.test.ts`)

| # | Test |
|---|------|
| 1 | `normalizeEdgeType` strips dashes, lowercases, removes non-alphanum characters |
| 2 | `normalizeCompoundId` strips `compound:` prefix then normalizes |
| 3 | `normalizeCompoundId` produces identical output for `BPC-157`, `bpc-157`, `bpc 157`, `bpc157` |
| 4 | `isRelationshipEdge` returns true for all whitelist entries |
| 5 | `isRelationshipEdge` returns false for taxonomic edges (`belongs-to-category`, `affects-pathway`) |
| 6 | `getRelationshipLabel` maps all expected edge types to user-facing copy |
| 7 | `getRelationshipLabel` returns `"Related compound"` for unknown types |
| 8 | `getEvidenceTierLabel` is case-insensitive (`STRONG`, `Strong`, `strong` all map the same) |
| 9 | `getEvidenceTierLabel` maps `anecdotal` to `"Community report — not clinically verified"` |
| 10 | `getEvidenceTierLabel` returns `"Evidence level unknown"` for null/undefined/empty |
| 11 | `getCommunitySignalLabel` returns strings for isolated/recurring/widespread |
| 12 | `getCommunitySignalLabel` returns null for `none` and absent/null |

### Page frame tests (`__tests__/app/knowledge/KnowledgeSlugPage.test.tsx`)

| # | Test |
|---|------|
| 1 | Renders `CompoundIntelligenceCard` when `getKnowledgeEntry` resolves |
| 2 | Does not import or call `getTrustLedger`, `TrustLedgerResponse`, or `CompoundDossier` (static source check) |
| 3 | Shows skeleton while loading |
| 4 | Shows rose error card when `getKnowledgeEntry` rejects |
| 5 | Does not render `CompoundRelationshipsSection` when entry is null (before load completes) |

### CompoundRelationshipsSection tests (`__tests__/components/knowledge/CompoundRelationshipsSection.test.tsx`)

| # | Test |
|---|------|
| 1 | Renders null when `fetchCompoundGraph` throws |
| 2 | Renders null when graph has no matching relationship edges |
| 3 | Filters out taxonomic edges (`belongs-to-category`, `affects-pathway`) |
| 4 | Matches compound by canonical name |
| 5 | Matches compound by alias |
| 6 | Matches compound by normalized node ID with `compound:` prefix stripped |
| 7 | Renders user-facing labels for `synergizes-with`, `conflicts-with`, `has-community-signal` — no raw edge type strings in DOM |
| 8 | Renders `"Community report — not clinically verified"` for Anecdotal evidence tier |
| 9 | Renders community signal badge for `recurring` and `widespread`; hides for `none`/absent |
| 10 | Renders `"Awaiting research review · Advisory signal only"` for `needsReview: true` |
| 11 | Does not render: `assertedRelationshipType`, `sourceAuthorityMix`, `canonicalTruthStatus`, raw `reviewFlags`, `sourceRefs`, `claimRefs`, raw `edgeType` |
| 12 | Links counterpart name to `/knowledge/[slug]` only when `node.nodeType === "compound"` and slug is safe |
| 13 | Sort order: stronger evidence tier first, then `needsReview: true` first, then counterpart label alphabetically |
| 14 | Skips malformed/partial edges without crashing; renders remaining valid edges |

### Admin regression test (add to `__tests__/components/research/CompoundRelationshipPanel.test.tsx`)

| # | Test |
|---|------|
| 15 | Admin panel still filters relationship edges and excludes taxonomic edges after shared helper extraction |

---

## Spec self-review

- **Placeholders:** None. All field names confirmed against live types.
- **Internal consistency:** Page frame uses `entry.canonicalName` (typed `string`, required). `CompoundRelationshipsSection` accepts `aliases?` (defensive) and handles `undefined` as `[]`. Sort order is deterministic. Link rule uses `node.nodeType === 'compound'` (exact typed value).
- **Scope:** One page fix + one new component + one shared module extraction. Focused.
- **Ambiguity:** `normalizeCompoundId` strips `compound:` prefix then applies `normalizeEdgeType` rules — same function, different input contract, named distinctly.
