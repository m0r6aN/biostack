# Research Review UI Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a four-page internal research review workbench under `/admin/research` that lets BioStack reviewers triage pipeline artifacts, inspect evidence claims, and export JSON review decisions.

**Architecture:** Flat Next.js 16 app-router route tree. A secured Next.js API route serves artifacts from either fixture files (default) or a configurable filesystem path (`RESEARCH_DATA_SOURCE=api`). A shared `components/research/` library and a `ReviewDecisionContext` are composed into four pages. All filters and sort on the compound list sync to URL query params.

**Tech Stack:** Next.js 16 (app router), TypeScript, Tailwind CSS, Vitest + `@testing-library/react`, existing GlassCard/glass design system.

---

## File Map

### New — infrastructure
| File | Responsibility |
|---|---|
| `frontend/src/lib/research/types.ts` | All TypeScript types derived from pipeline schemas |
| `frontend/src/lib/research/slugs.ts` | `toSlug()` + `buildSlugMap()` |
| `frontend/src/lib/research/loader.ts` | Typed fetch wrappers per artifact |
| `frontend/src/lib/research/reviewDecisionBatch.ts` | Immutable batch accumulator + JSON export |
| `frontend/src/lib/research/ReviewDecisionContext.tsx` | React context for in-session decision batch |
| `frontend/src/app/api/research/artifacts/route.ts` | Secured API route — auth guard + fixed allowlist |

### New — fixtures (7 files)
`frontend/src/fixtures/research/` — `research-summary.json`, `promotion-manifest.json`, `review-resolution-plan.json`, `promotion-import-preview.json`, `import-dry-run/promotion-import-dry-run-report.json`, `promotion-export/promotion-export-manifest.json`, `promotion-export/substances.promotable.json`

### New — shared components
`frontend/src/components/research/` — `ResearchStatChip.tsx`, `ReadinessBadge.tsx`, `BlockerCard.tsx`, `CompoundCard.tsx`, `FilterBar.tsx`, `ClaimCard.tsx`, `ResolutionPlanItem.tsx`, `ReviewDecisionForm.tsx`

### New — pages + layout
| File | Route |
|---|---|
| `frontend/src/app/admin/research/layout.tsx` | Provides `ReviewDecisionContext` to all research pages |
| `frontend/src/app/admin/research/page.tsx` | `/admin/research` — dashboard |
| `frontend/src/app/admin/research/compounds/page.tsx` | `/admin/research/compounds` — list |
| `frontend/src/app/admin/research/compounds/[slug]/page.tsx` | `/admin/research/compounds/[slug]` — detail |
| `frontend/src/app/admin/research/pipeline/page.tsx` | `/admin/research/pipeline` — pipeline panel |

### Modified
| File | Change |
|---|---|
| `frontend/src/lib/utils.ts` | Add `Insufficient`, `Anecdotal`, `Unknown` to `getEvidenceTierColor` |
| `frontend/src/components/knowledge/EvidenceTierBadge.tsx` | Add `variant='research'` + `researchTierLabels` |
| `frontend/src/components/Sidebar.tsx` | Add Research nav item; fix Admin active-match to exact |

### Tests
```
frontend/src/__tests__/lib/research/slugs.test.ts
frontend/src/__tests__/lib/research/reviewDecisionBatch.test.ts
frontend/src/__tests__/components/research/ReadinessBadge.test.tsx
frontend/src/__tests__/components/research/BlockerCard.test.tsx
frontend/src/__tests__/components/research/CompoundCard.test.tsx
frontend/src/__tests__/components/research/FilterBar.test.tsx
frontend/src/__tests__/components/research/ClaimCard.test.tsx
frontend/src/__tests__/components/research/ResolutionPlanItem.test.tsx
frontend/src/__tests__/components/research/ReviewDecisionForm.test.tsx
frontend/src/__tests__/components/research/ResearchDashboard.test.tsx
frontend/src/__tests__/components/research/CompoundList.test.tsx
frontend/src/__tests__/components/research/CompoundDetail.test.tsx
```

---

## Task 1: TypeScript Types

**Files:**
- Create: `frontend/src/lib/research/types.ts`

- [ ] **Step 1: Create the types file**

```ts
// frontend/src/lib/research/types.ts

export type PromotionReadiness =
  | 'blocked'
  | 'review-required'
  | 'candidate-for-promotion';

export type EvidenceTier =
  | 'Strong' | 'Moderate' | 'Limited'
  | 'Insufficient' | 'Unknown' | 'Anecdotal';

export type Completeness = 'minimal' | 'partial' | 'substantial' | 'complete';

export type ClaimType =
  | 'identity' | 'regulatory' | 'approved-indication' | 'studied-use'
  | 'common-off-label-use' | 'mechanism' | 'target-pathway' | 'efficacy'
  | 'dose-context' | 'formulation' | 'storage-reconstitution'
  | 'contraindication' | 'warning' | 'adverse-effect' | 'monitoring'
  | 'interaction' | 'stack-heuristic' | 'misinformation-claim'
  | 'evidence-gap' | 'controversy';

export type Confidence = 'low' | 'moderate' | 'high' | 'unknown';

export type ReviewDecisionType =
  | 'approve-for-promotion' | 'approve-claims'
  | 'request-changes' | 'reject';

// ── Research Summary ──────────────────────────────────────────────────────────
export interface ResearchSummary {
  draftSubstanceCount: number;
  reviewQueueItemCount: number;
  compounds: ResearchSummaryCompound[];
  reviewCategories: ResearchReviewCategory[];
  promotionReadiness: ResearchSummaryBucket[];
  qualityFlags: ResearchSummaryBucket[];
  reviewReasons: ResearchSummaryBucket[];
  classifications: ResearchSummaryBucket[];
  evidenceTiers: ResearchSummaryBucket[];
}

export interface ResearchSummaryCompound {
  name: string;
  classification: string;
  overallEvidenceTier: string;
  completeness: string;
  needsReview: boolean;
  reviewQueueItemCount: number;
  promotionReadiness: PromotionReadiness;
  promotionBlockers: string[];
  reviewDecisionIds: string[];
  qualityFlags: string[];
  reviewReasons: string[];
}

export interface ResearchReviewCategory {
  name: string;
  count: number;
  compounds: string[];
  signals: string[];
  recommendedActions: string[];
}

export interface ResearchSummaryBucket {
  name: string;
  count: number;
  compounds: string[];
}

// ── Promotion Manifest ────────────────────────────────────────────────────────
export interface PromotionManifest {
  manifestVersion: string;
  generatedAtUtc: string;
  counts: PromotionManifestCounts;
  blocked: PromotionManifestCandidate[];
  reviewRequired: PromotionManifestCandidate[];
  candidatesForPromotion: PromotionManifestCandidate[];
}

export interface PromotionManifestCounts {
  totalDrafts: number;
  blocked: number;
  reviewRequired: number;
  candidatesForPromotion: number;
}

export interface PromotionManifestCandidate {
  name: string;
  classification: string;
  readiness: PromotionReadiness;
  overallEvidenceTier: string;
  completeness: string;
  reviewQueueItemCount: number;
  reviewDecisionIds: string[];
  blockers: string[];
  qualityFlags: string[];
  requiredNextActions: string[];
}

// ── Review Resolution Plan ────────────────────────────────────────────────────
export interface ReviewResolutionPlan {
  planVersion: string;
  generatedAtUtc: string;
  counts: ReviewResolutionPlanCounts;
  items: ReviewResolutionPlanItem[];
}

export interface ReviewResolutionPlanCounts {
  totalItems: number;
  blockedItems: number;
  reviewRequiredItems: number;
  resolutionTypes: ResearchSummaryBucket[];
}

export interface ReviewResolutionPlanItem {
  itemId: string;
  compoundName: string;
  readiness: string;
  severity: string;
  resolutionType: string;
  issue: string;
  recommendedAction: string;
  relatedBlockers: string[];
  relatedQualityFlags: string[];
}

// ── Evidence Packet ───────────────────────────────────────────────────────────
export interface EvidencePacket {
  schemaVersion: string;
  recordType: 'compound-evidence-packet';
  packet: { packetId: string; category: string; agentId: string; generatedAt: string; sourceRegistryVersion: string };
  compound: {
    canonicalName: string;
    aliases: string[];
    classification: string;
    compoundFamily: string | null;
    externalIdentifiers: Record<string, string | null>;
  };
  sources: EvidenceSource[];
  claims: EvidenceClaim[];
  conflicts: EvidenceConflict[];
  ops: { completeness: Completeness; needsReview: boolean; reviewReasons: string[]; qualityFlags: string[] };
}

export interface EvidenceSource {
  sourceId: string;
  sourceType: string;
  authorityTier: string;
  title: string;
  publisher: string | null;
  url: string | null;
  doi: string | null;
  pmid: string | null;
  publishedAt: string | null;
  accessedAt: string;
}

export interface EvidenceClaim {
  claimId: string;
  claimType: ClaimType;
  statement: string;
  context: {
    population: string | null;
    route: string | null;
    formulation: string | null;
    useCase: string | null;
    doseText: string | null;
  };
  evidenceTier: EvidenceTier;
  confidence: Confidence;
  fieldAuthorityRequired: boolean;
  sourceRefs: string[];
  extractedEvidence: Array<{ sourceRef: string; quote: string | null; pageOrSection: string | null }>;
  reviewFlags: string[];
}

export interface EvidenceConflict {
  conflictId: string;
  claimRefs: string[];
  severity: 'low' | 'moderate' | 'high' | 'critical' | 'review';
  summary: string;
  resolutionStatus: 'unresolved' | 'resolved' | 'needs-human-review';
}

// ── Promotion Import Preview ──────────────────────────────────────────────────
export interface PromotionImportPreview {
  previewVersion: string;
  generatedAtUtc: string;
  counts: {
    totalExported: number; wouldCreate: number; wouldUpdate: number; wouldSkip: number;
    schemaValid: number; schemaInvalid: number; duplicateSlugs: number;
    duplicateCanonicalIds: number; activeRecords: number; inactiveRecords: number;
  };
  items: PromotionImportPreviewItem[];
}

export interface PromotionImportPreviewItem {
  name: string;
  slug: string;
  canonicalId: string;
  action: 'create' | 'update' | 'skip';
  schemaValid: boolean;
  isActive: boolean;
  existingSeedMatch: boolean;
  reasons: string[];
  reviewDecisionIds: string[];
}

// ── Review Decision ───────────────────────────────────────────────────────────
export interface ReviewDecisionScope {
  claimIds: string[];
  qualityFlags: string[];
  reviewCategories: string[];
  promotionBlockers: string[];
}

export interface ReviewDecision {
  decisionId: string;
  compoundName: string;
  decision: ReviewDecisionType;
  reviewerId: string;
  reviewedAt: string;
  scope: ReviewDecisionScope;
  clearsSoftPromotionBlockers: boolean;
  expiresAt: string | null;
  notes: string[];
}

export interface ReviewDecisionBatch {
  schemaVersion: '1.0.0';
  recordType: 'review-decision-batch';
  batch: { batchId: string; reviewerId: string; reviewedAt: string; notes: string[] };
  decisions: ReviewDecision[];
}

export type SlugMap = Map<string, string>; // slug → canonicalName
```

- [ ] **Step 2: Commit**

```bash
git add frontend/src/lib/research/types.ts
git commit -m "feat(research-ui): TypeScript types for all pipeline artifacts"
```

---

## Task 2: Slug Utility

**Files:**
- Create: `frontend/src/lib/research/slugs.ts`
- Create: `frontend/src/__tests__/lib/research/slugs.test.ts`

- [ ] **Step 1: Write failing tests**

```ts
// frontend/src/__tests__/lib/research/slugs.test.ts
import { toSlug, buildSlugMap } from '@/lib/research/slugs';
import type { ResearchSummaryCompound } from '@/lib/research/types';

describe('toSlug', () => {
  it('lowercases and replaces spaces with hyphens', () => {
    expect(toSlug('Testosterone cypionate')).toBe('testosterone-cypionate');
  });
  it('handles hyphens already present', () => {
    expect(toSlug('GHK-Cu')).toBe('ghk-cu');
  });
  it('handles numbers', () => {
    expect(toSlug('BPC-157')).toBe('bpc-157');
  });
  it('strips parentheses and punctuation', () => {
    expect(toSlug('Vitamin D3 (cholecalciferol)')).toBe('vitamin-d3-cholecalciferol');
  });
  it('collapses consecutive hyphens', () => {
    expect(toSlug('A -- B')).toBe('a-b');
  });
  it('strips leading/trailing whitespace', () => {
    expect(toSlug(' Creatine ')).toBe('creatine');
  });
});

describe('buildSlugMap', () => {
  const compounds: Pick<ResearchSummaryCompound, 'name'>[] = [
    { name: 'BPC-157' },
    { name: 'Testosterone cypionate' },
    { name: 'Creatine' },
  ];

  it('maps slug to canonical name', () => {
    const map = buildSlugMap(compounds);
    expect(map.get('bpc-157')).toBe('BPC-157');
    expect(map.get('testosterone-cypionate')).toBe('Testosterone cypionate');
    expect(map.get('creatine')).toBe('Creatine');
  });

  it('returns undefined for unknown slug', () => {
    expect(buildSlugMap(compounds).get('unknown-xyz')).toBeUndefined();
  });
});
```

- [ ] **Step 2: Run — verify FAIL**

```bash
cd frontend && npx vitest run src/__tests__/lib/research/slugs.test.ts
```
Expected: `Cannot find module '@/lib/research/slugs'`

- [ ] **Step 3: Implement**

```ts
// frontend/src/lib/research/slugs.ts
export function toSlug(canonicalName: string): string {
  return canonicalName
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/-+/g, '-')
    .replace(/^-|-$/g, '');
}

export function buildSlugMap(
  compounds: ReadonlyArray<{ name: string }>
): Map<string, string> {
  const map = new Map<string, string>();
  for (const c of compounds) map.set(toSlug(c.name), c.name);
  return map;
}
```

- [ ] **Step 4: Run — verify PASS**

```bash
cd frontend && npx vitest run src/__tests__/lib/research/slugs.test.ts
```
Expected: 8 tests pass.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/lib/research/slugs.ts frontend/src/__tests__/lib/research/slugs.test.ts
git commit -m "feat(research-ui): slug utility with round-trip tests"
```

---

## Task 3: ReviewDecisionBatch Utility

**Files:**
- Create: `frontend/src/lib/research/reviewDecisionBatch.ts`
- Create: `frontend/src/__tests__/lib/research/reviewDecisionBatch.test.ts`

- [ ] **Step 1: Write failing tests**

```ts
// frontend/src/__tests__/lib/research/reviewDecisionBatch.test.ts
import { createBatch, addDecision, toJson } from '@/lib/research/reviewDecisionBatch';
import type { ReviewDecision } from '@/lib/research/types';

const makeDecision = (overrides: Partial<ReviewDecision> = {}): ReviewDecision => ({
  decisionId: 'dec-001',
  compoundName: 'BPC-157',
  decision: 'request-changes',
  reviewerId: 'reviewer-1',
  reviewedAt: '2026-05-05T10:00:00Z',
  scope: { claimIds: [], qualityFlags: [], reviewCategories: [], promotionBlockers: [] },
  clearsSoftPromotionBlockers: false,
  expiresAt: null,
  notes: ['Needs authoritative source'],
  ...overrides,
});

describe('createBatch', () => {
  it('creates empty batch with given reviewerId', () => {
    const b = createBatch('reviewer-1');
    expect(b.decisions).toHaveLength(0);
    expect(b.batch.reviewerId).toBe('reviewer-1');
    expect(b.recordType).toBe('review-decision-batch');
    expect(b.schemaVersion).toBe('1.0.0');
  });
});

describe('addDecision', () => {
  it('appends a decision', () => {
    const b = addDecision(createBatch('reviewer-1'), makeDecision());
    expect(b.decisions).toHaveLength(1);
    expect(b.decisions[0].compoundName).toBe('BPC-157');
  });

  it('does not mutate the original', () => {
    const original = createBatch('reviewer-1');
    addDecision(original, makeDecision());
    expect(original.decisions).toHaveLength(0);
  });

  it('accumulates multiple decisions', () => {
    let b = createBatch('reviewer-1');
    b = addDecision(b, makeDecision({ decisionId: 'dec-001', compoundName: 'BPC-157' }));
    b = addDecision(b, makeDecision({ decisionId: 'dec-002', compoundName: 'Creatine' }));
    expect(b.decisions).toHaveLength(2);
  });
});

describe('toJson', () => {
  it('returns valid JSON matching schema structure', () => {
    const b = addDecision(createBatch('reviewer-1'), makeDecision());
    const parsed = JSON.parse(toJson(b));
    expect(parsed.schemaVersion).toBe('1.0.0');
    expect(parsed.recordType).toBe('review-decision-batch');
    expect(parsed.decisions).toHaveLength(1);
  });

  it('pretty-prints', () => {
    expect(toJson(createBatch('r'))).toContain('\n');
  });
});
```

- [ ] **Step 2: Run — verify FAIL**

```bash
cd frontend && npx vitest run src/__tests__/lib/research/reviewDecisionBatch.test.ts
```

- [ ] **Step 3: Implement**

```ts
// frontend/src/lib/research/reviewDecisionBatch.ts
import type { ReviewDecision, ReviewDecisionBatch } from './types';

export function createBatch(reviewerId: string): ReviewDecisionBatch {
  return {
    schemaVersion: '1.0.0',
    recordType: 'review-decision-batch',
    batch: {
      batchId: `batch-${Date.now()}`,
      reviewerId,
      reviewedAt: new Date().toISOString(),
      notes: [],
    },
    decisions: [],
  };
}

export function addDecision(
  batch: ReviewDecisionBatch,
  decision: ReviewDecision
): ReviewDecisionBatch {
  return { ...batch, decisions: [...batch.decisions, decision] };
}

export function toJson(batch: ReviewDecisionBatch): string {
  return JSON.stringify(batch, null, 2);
}
```

- [ ] **Step 4: Run — verify PASS**

```bash
cd frontend && npx vitest run src/__tests__/lib/research/reviewDecisionBatch.test.ts
```
Expected: 7 tests pass.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/lib/research/reviewDecisionBatch.ts frontend/src/__tests__/lib/research/reviewDecisionBatch.test.ts
git commit -m "feat(research-ui): ReviewDecisionBatch — immutable accumulator + JSON export"
```

---

## Task 4: Fixture Data

**Files:** 7 JSON files under `frontend/src/fixtures/research/`

- [ ] **Step 1: Create `research-summary.json`**

```json
{
  "draftSubstanceCount": 3,
  "reviewQueueItemCount": 5,
  "compounds": [
    {
      "name": "BPC-157",
      "classification": "Peptide",
      "overallEvidenceTier": "Limited",
      "completeness": "partial",
      "needsReview": true,
      "reviewQueueItemCount": 3,
      "promotionReadiness": "blocked",
      "promotionBlockers": [
        "blocked: missing required authoritative support",
        "review-required: draft is marked needsReview",
        "review-required: completeness is partial"
      ],
      "reviewDecisionIds": [],
      "qualityFlags": ["missing-authoritative-support", "limited-human-evidence", "misinformation-heavy"],
      "reviewReasons": ["requires authoritative A1/A2 source for safety claims", "needsReview flag set by pipeline"]
    },
    {
      "name": "Semaglutide",
      "classification": "Pharmaceutical",
      "overallEvidenceTier": "Strong",
      "completeness": "substantial",
      "needsReview": true,
      "reviewQueueItemCount": 2,
      "promotionReadiness": "review-required",
      "promotionBlockers": [
        "review-required: draft is marked needsReview",
        "review-required: 2 review queue item(s)"
      ],
      "reviewDecisionIds": [],
      "qualityFlags": ["a1-label-backed", "product-specific-labels"],
      "reviewReasons": ["regulatory boundary — approved product with specific label", "needsReview flag set by pipeline"]
    },
    {
      "name": "Creatine",
      "classification": "Supplement",
      "overallEvidenceTier": "Strong",
      "completeness": "substantial",
      "needsReview": false,
      "reviewQueueItemCount": 0,
      "promotionReadiness": "candidate-for-promotion",
      "promotionBlockers": [],
      "reviewDecisionIds": [],
      "qualityFlags": [],
      "reviewReasons": []
    }
  ],
  "reviewCategories": [
    {
      "name": "Safety Critical",
      "count": 1,
      "compounds": ["BPC-157"],
      "signals": ["missing-authoritative-support"],
      "recommendedActions": [
        "Require A1/A2 support for contraindications, warnings, adverse effects, monitoring, and safety-critical fields.",
        "Escalate prescription, hormone-axis, and severe-risk claims for human review."
      ]
    },
    {
      "name": "Misinformation / Vendor Claims",
      "count": 1,
      "compounds": ["BPC-157"],
      "signals": ["misinformation-heavy"],
      "recommendedActions": [
        "Treat social, vendor, and community claims as popularity or misinformation signals only.",
        "Use cautious customer-facing language that separates claimed uses from supported evidence."
      ]
    },
    {
      "name": "Regulatory / Approval",
      "count": 1,
      "compounds": ["Semaglutide"],
      "signals": ["a1-label-backed", "product-specific-labels"],
      "recommendedActions": [
        "Verify jurisdiction-specific status against authoritative regulator or product-label sources.",
        "Do not publish approved-use language unless the exact product/use case is source-backed."
      ]
    }
  ],
  "promotionReadiness": [
    { "name": "blocked", "count": 1, "compounds": ["BPC-157"] },
    { "name": "review-required", "count": 1, "compounds": ["Semaglutide"] },
    { "name": "candidate-for-promotion", "count": 1, "compounds": ["Creatine"] }
  ],
  "qualityFlags": [
    { "name": "missing-authoritative-support", "count": 1, "compounds": ["BPC-157"] },
    { "name": "misinformation-heavy", "count": 1, "compounds": ["BPC-157"] },
    { "name": "limited-human-evidence", "count": 1, "compounds": ["BPC-157"] },
    { "name": "a1-label-backed", "count": 1, "compounds": ["Semaglutide"] },
    { "name": "product-specific-labels", "count": 1, "compounds": ["Semaglutide"] }
  ],
  "reviewReasons": [
    { "name": "requires authoritative A1/A2 source for safety claims", "count": 1, "compounds": ["BPC-157"] },
    { "name": "needsReview flag set by pipeline", "count": 2, "compounds": ["BPC-157", "Semaglutide"] }
  ],
  "classifications": [
    { "name": "Peptide", "count": 1, "compounds": ["BPC-157"] },
    { "name": "Pharmaceutical", "count": 1, "compounds": ["Semaglutide"] },
    { "name": "Supplement", "count": 1, "compounds": ["Creatine"] }
  ],
  "evidenceTiers": [
    { "name": "Strong", "count": 2, "compounds": ["Semaglutide", "Creatine"] },
    { "name": "Limited", "count": 1, "compounds": ["BPC-157"] }
  ]
}
```

- [ ] **Step 2: Create `promotion-manifest.json`**

```json
{
  "manifestVersion": "1.0.0",
  "generatedAtUtc": "2026-05-05T09:00:00Z",
  "counts": { "totalDrafts": 3, "blocked": 1, "reviewRequired": 1, "candidatesForPromotion": 1 },
  "blocked": [
    {
      "name": "BPC-157",
      "classification": "Peptide",
      "readiness": "blocked",
      "overallEvidenceTier": "Limited",
      "completeness": "partial",
      "reviewQueueItemCount": 3,
      "reviewDecisionIds": [],
      "blockers": [
        "blocked: missing required authoritative support",
        "review-required: draft is marked needsReview",
        "review-required: completeness is partial"
      ],
      "qualityFlags": ["missing-authoritative-support", "limited-human-evidence", "misinformation-heavy"],
      "requiredNextActions": [
        "Attach an A1/A2 source for safety, regulatory, monitoring, contraindication, or approved-use claim before promotion.",
        "Resolve all blocked promotion blockers before review approval or import promotion."
      ]
    }
  ],
  "reviewRequired": [
    {
      "name": "Semaglutide",
      "classification": "Pharmaceutical",
      "readiness": "review-required",
      "overallEvidenceTier": "Strong",
      "completeness": "substantial",
      "reviewQueueItemCount": 2,
      "reviewDecisionIds": [],
      "blockers": [
        "review-required: draft is marked needsReview",
        "review-required: 2 review queue item(s)"
      ],
      "qualityFlags": ["a1-label-backed", "product-specific-labels"],
      "requiredNextActions": [
        "Complete human review and clear promotion blockers before marking candidate-for-promotion.",
        "Verify the latest product label before publication."
      ]
    }
  ],
  "candidatesForPromotion": [
    {
      "name": "Creatine",
      "classification": "Supplement",
      "readiness": "candidate-for-promotion",
      "overallEvidenceTier": "Strong",
      "completeness": "substantial",
      "reviewQueueItemCount": 0,
      "reviewDecisionIds": [],
      "blockers": [],
      "qualityFlags": [],
      "requiredNextActions": [
        "Eligible for promotion review; verify final provenance and schema output before import."
      ]
    }
  ]
}
```

- [ ] **Step 3: Create `review-resolution-plan.json`**

```json
{
  "planVersion": "1.0.0",
  "generatedAtUtc": "2026-05-05T09:00:00Z",
  "counts": {
    "totalItems": 3,
    "blockedItems": 1,
    "reviewRequiredItems": 2,
    "resolutionTypes": [
      { "name": "add-authoritative-source", "count": 1, "compounds": ["BPC-157"] },
      { "name": "human-review", "count": 2, "compounds": ["BPC-157", "Semaglutide"] }
    ]
  },
  "items": [
    {
      "itemId": "bpc-157-resolution-1",
      "compoundName": "BPC-157",
      "readiness": "blocked",
      "severity": "blocked",
      "resolutionType": "add-authoritative-source",
      "issue": "blocked: missing required authoritative support",
      "recommendedAction": "Attach an A1/A2 source for the safety, regulatory, monitoring, contraindication, or approved-use claim before promotion.",
      "relatedBlockers": ["blocked: missing required authoritative support"],
      "relatedQualityFlags": ["missing-authoritative-support"]
    },
    {
      "itemId": "bpc-157-resolution-2",
      "compoundName": "BPC-157",
      "readiness": "blocked",
      "severity": "review",
      "resolutionType": "human-review",
      "issue": "review-required: draft is marked needsReview",
      "recommendedAction": "Human reviewer must assess and resolve: review-required: draft is marked needsReview",
      "relatedBlockers": ["review-required: draft is marked needsReview"],
      "relatedQualityFlags": ["limited-human-evidence"]
    },
    {
      "itemId": "semaglutide-resolution-1",
      "compoundName": "Semaglutide",
      "readiness": "review-required",
      "severity": "review",
      "resolutionType": "human-review",
      "issue": "review-required: 2 review queue item(s)",
      "recommendedAction": "Human reviewer must assess and resolve each review queue item.",
      "relatedBlockers": ["review-required: 2 review queue item(s)"],
      "relatedQualityFlags": ["a1-label-backed"]
    }
  ]
}
```

- [ ] **Step 4: Create remaining fixture files**

`frontend/src/fixtures/research/promotion-import-preview.json`:
```json
{
  "previewVersion": "1.0.0",
  "generatedAtUtc": "2026-05-05T09:00:00Z",
  "counts": {
    "totalExported": 1, "wouldCreate": 1, "wouldUpdate": 0, "wouldSkip": 0,
    "schemaValid": 1, "schemaInvalid": 0, "duplicateSlugs": 0,
    "duplicateCanonicalIds": 0, "activeRecords": 1, "inactiveRecords": 0
  },
  "items": [
    {
      "name": "Creatine", "slug": "creatine",
      "canonicalId": "creatine-001",
      "action": "create", "schemaValid": true, "isActive": true,
      "existingSeedMatch": false, "reasons": ["New compound — no existing seed record found."],
      "reviewDecisionIds": []
    }
  ]
}
```

`frontend/src/fixtures/research/import-dry-run/promotion-import-dry-run-report.json`:
```json
{
  "reportVersion": "1.0.0",
  "generatedAtUtc": "2026-05-05T09:00:00Z",
  "summary": { "total": 1, "safe": 1, "blocked": 0, "errors": 0 },
  "items": [
    { "name": "Creatine", "slug": "creatine", "safe": true, "action": "create", "errors": [], "warnings": [] }
  ]
}
```

`frontend/src/fixtures/research/promotion-export/promotion-export-manifest.json`:
```json
{
  "exportVersion": "1.0.0",
  "generatedAtUtc": "2026-05-05T09:00:00Z",
  "exportedCount": 1,
  "schemaVersion": "1.0.0",
  "outputFiles": ["substances.promotable.json"]
}
```

`frontend/src/fixtures/research/promotion-export/substances.promotable.json`:
```json
[
  {
    "identity": {
      "canonicalName": "Creatine",
      "slug": "creatine",
      "classification": "Supplement"
    }
  }
]
```

- [ ] **Step 5: Commit**

```bash
git add frontend/src/fixtures/research/
git commit -m "feat(research-ui): fixture data for 3 representative pilot compounds"
```

---

## Task 5: API Route + Data Loader

**Files:**
- Create: `frontend/src/app/api/research/artifacts/route.ts`
- Create: `frontend/src/lib/research/loader.ts`

- [ ] **Step 1: Create the API route**

```ts
// frontend/src/app/api/research/artifacts/route.ts
import { NextRequest } from 'next/server';
import { readFile } from 'fs/promises';
import path from 'path';

const ALLOWED: Record<string, string> = {
  'research-summary': 'research-summary.json',
  'promotion-manifest': 'promotion-manifest.json',
  'review-resolution-plan': 'review-resolution-plan.json',
  'promotion-import-preview': 'promotion-import-preview.json',
  'import-dry-run/promotion-import-dry-run-report': 'import-dry-run/promotion-import-dry-run-report.json',
  'promotion-export/promotion-export-manifest': 'promotion-export/promotion-export-manifest.json',
  'promotion-export/substances.promotable': 'promotion-export/substances.promotable.json',
};

export async function GET(request: NextRequest) {
  if (process.env.NODE_ENV === 'production') {
    return Response.json({ error: 'Not Found' }, { status: 404 });
  }

  const auth = request.headers.get('authorization');
  if (!auth?.startsWith('Bearer ')) {
    return Response.json({ error: 'Unauthorized' }, { status: 401 });
  }

  const artifact = request.nextUrl.searchParams.get('artifact') ?? '';
  const filename = ALLOWED[artifact];
  if (!filename) {
    return Response.json({ error: 'Invalid artifact' }, { status: 400 });
  }

  const dataSource = process.env.RESEARCH_DATA_SOURCE ?? 'fixtures';
  const projectRoot = path.resolve(process.cwd(), '..');

  let basePath: string;
  if (dataSource === 'api') {
    const artifactsPath = process.env.RESEARCH_ARTIFACTS_PATH ?? 'research/pilot';
    basePath = path.resolve(process.cwd(), '..', artifactsPath);
    if (!basePath.startsWith(projectRoot)) {
      return Response.json({ error: 'Invalid path' }, { status: 400 });
    }
  } else {
    basePath = path.resolve(process.cwd(), 'src/fixtures/research');
  }

  try {
    const content = await readFile(path.join(basePath, filename), 'utf-8');
    return Response.json(JSON.parse(content));
  } catch {
    return Response.json({ error: 'Artifact not found' }, { status: 404 });
  }
}
```

- [ ] **Step 2: Create the data loader**

```ts
// frontend/src/lib/research/loader.ts
import type {
  ResearchSummary, PromotionManifest, ReviewResolutionPlan, PromotionImportPreview,
} from './types';

async function fetchArtifact<T>(artifact: string, token: string): Promise<T> {
  const res = await fetch(
    `/api/research/artifacts?artifact=${encodeURIComponent(artifact)}`,
    { headers: { Authorization: `Bearer ${token}` } }
  );
  if (!res.ok) throw new Error(`Failed to fetch ${artifact}: ${res.status}`);
  return res.json() as Promise<T>;
}

export const fetchResearchSummary = (t: string) =>
  fetchArtifact<ResearchSummary>('research-summary', t);

export const fetchPromotionManifest = (t: string) =>
  fetchArtifact<PromotionManifest>('promotion-manifest', t);

export const fetchReviewResolutionPlan = (t: string) =>
  fetchArtifact<ReviewResolutionPlan>('review-resolution-plan', t);

export const fetchImportPreview = (t: string) =>
  fetchArtifact<PromotionImportPreview>('promotion-import-preview', t);

export const fetchDryRunReport = (t: string) =>
  fetchArtifact<unknown>('import-dry-run/promotion-import-dry-run-report', t);

export const fetchExportManifest = (t: string) =>
  fetchArtifact<unknown>('promotion-export/promotion-export-manifest', t);

export const fetchPromotableSubstances = (t: string) =>
  fetchArtifact<unknown[]>('promotion-export/substances.promotable', t);
```

- [ ] **Step 3: Commit**

```bash
git add frontend/src/app/api/research/artifacts/route.ts frontend/src/lib/research/loader.ts
git commit -m "feat(research-ui): secured API route + typed data loader"
```

---

## Task 6: EvidenceTierBadge Extension + Sidebar

**Files:**
- Modify: `frontend/src/lib/utils.ts`
- Modify: `frontend/src/components/knowledge/EvidenceTierBadge.tsx`
- Modify: `frontend/src/components/Sidebar.tsx`

- [ ] **Step 1: Add missing pipeline tiers to `getEvidenceTierColor` in `utils.ts`**

Add four new cases before the `default` in the `getEvidenceTierColor` switch:

```ts
// In the switch inside getEvidenceTierColor, add after 'theoretical':
case 'insufficient':
  return 'bg-rose-500/10 text-rose-300 border border-rose-400/20';
case 'anecdotal':
  return 'bg-white/5 text-white/40 border border-white/10';
case 'unknown':
  return 'bg-white/5 text-white/40 border border-white/10';
```

- [ ] **Step 2: Extend `EvidenceTierBadge` with a `research` variant**

Replace the file content with:

```tsx
// frontend/src/components/knowledge/EvidenceTierBadge.tsx
import { getEvidenceTierColor } from '@/lib/utils';

interface EvidenceTierBadgeProps {
  tier: string;
  variant?: 'default' | 'research';
}

const labels: Record<string, string> = {
  strong: 'Strong Evidence',
  moderate: 'Moderate Evidence',
  limited: 'Limited Evidence',
  theoretical: 'Theoretical',
};

const researchTierLabels: Record<string, string> = {
  strong: 'Strong',
  moderate: 'Moderate',
  limited: 'Limited',
  insufficient: 'Insufficient',
  unknown: 'Unknown',
  anecdotal: 'Anecdotal',
};

export function EvidenceTierBadge({ tier, variant = 'default' }: EvidenceTierBadgeProps) {
  const lower = tier.toLowerCase();
  const map = variant === 'research' ? researchTierLabels : labels;
  return (
    <span className={`text-xs font-medium px-2.5 py-1 rounded-full ${getEvidenceTierColor(lower)}`}>
      {map[lower] ?? tier}
    </span>
  );
}
```

- [ ] **Step 3: Add Research nav item to `Sidebar.tsx`**

Add the icon function before the `navItems` array:

```tsx
function IconResearch() {
  return (
    <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.35" strokeLinecap="round" strokeLinejoin="round" className="w-4 h-4">
      <circle cx="6.5" cy="6.5" r="4" />
      <line x1="9.5" y1="9.5" x2="14" y2="14" />
      <line x1="6.5" y1="4" x2="6.5" y2="9" />
      <line x1="4" y1="6.5" x2="9" y2="6.5" />
    </svg>
  );
}
```

Update the `navItems` array — add `exact` property and Research entry. Replace the Admin and add Research after it:

```ts
const navItems = [
  { label: 'Protocol Console', href: '/protocol-console', icon: <IconProtocolConsole />, adminOnly: false, exact: false },
  { label: 'Profiles',    href: '/profiles',    icon: <IconProfiles />,    adminOnly: false, exact: false },
  { label: 'Compounds',   href: '/compounds',   icon: <IconCompounds />,   adminOnly: false, exact: false },
  { label: 'Protocols',   href: '/protocols',   icon: <IconProtocols />,   adminOnly: false, exact: false },
  { label: 'Check-ins',   href: '/checkins',    icon: <IconCheckins />,    adminOnly: false, exact: false },
  { label: 'Timeline',    href: '/timeline',    icon: <IconTimeline />,    adminOnly: false, exact: false },
  { label: 'Tools',       href: '/tools',       icon: <IconCalculators />, adminOnly: false, exact: false },
  { label: 'Knowledge',   href: '/knowledge',   icon: <IconKnowledge />,   adminOnly: false, exact: false },
  { label: 'Billing',     href: '/billing',     icon: <IconBilling />,     adminOnly: false, exact: false },
  { label: 'Admin',       href: '/admin',       icon: <IconAdmin />,       adminOnly: true,  exact: true  },
  { label: 'Research',    href: '/admin/research', icon: <IconResearch />, adminOnly: true,  exact: false },
];
```

Update the `isActive` computation inside the map:

```tsx
const isActive =
  item.href === '/protocol-console'
    ? pathname.startsWith('/protocol-console') || pathname.startsWith('/mission-control')
    : item.exact
      ? pathname === item.href
      : pathname.startsWith(item.href);
```

- [ ] **Step 4: Verify no existing tests break**

```bash
cd frontend && npx vitest run src/__tests__/components/EvidenceTierBadge 2>/dev/null; npx vitest run src/__tests__/components/SimpleComponents
```

- [ ] **Step 5: Commit**

```bash
git add frontend/src/lib/utils.ts frontend/src/components/knowledge/EvidenceTierBadge.tsx frontend/src/components/Sidebar.tsx
git commit -m "feat(research-ui): research EvidenceTier variants, Research nav link, Admin exact-match fix"
```

---

## Task 7: ReviewDecisionContext

**Files:**
- Create: `frontend/src/lib/research/ReviewDecisionContext.tsx`
- Create: `frontend/src/app/admin/research/layout.tsx`

- [ ] **Step 1: Create the context**

```tsx
// frontend/src/lib/research/ReviewDecisionContext.tsx
'use client';
import { createContext, useContext, useState, type ReactNode } from 'react';
import type { ReviewDecision, ReviewDecisionBatch } from './types';
import { createBatch, addDecision } from './reviewDecisionBatch';

interface ReviewDecisionContextValue {
  batch: ReviewDecisionBatch;
  reviewerId: string;
  setReviewerId: (id: string) => void;
  addToSession: (decision: ReviewDecision) => void;
}

const ReviewDecisionContext = createContext<ReviewDecisionContextValue | null>(null);

export function ReviewDecisionProvider({ children }: { children: ReactNode }) {
  const [reviewerId, setReviewerIdState] = useState('');
  const [batch, setBatch] = useState<ReviewDecisionBatch>(() => createBatch(''));

  function setReviewerId(id: string) {
    setReviewerIdState(id);
    setBatch(prev => ({ ...prev, batch: { ...prev.batch, reviewerId: id } }));
  }

  function addToSession(decision: ReviewDecision) {
    setBatch(prev => addDecision(prev, decision));
  }

  return (
    <ReviewDecisionContext.Provider value={{ batch, reviewerId, setReviewerId, addToSession }}>
      {children}
    </ReviewDecisionContext.Provider>
  );
}

export function useReviewDecision() {
  const ctx = useContext(ReviewDecisionContext);
  if (!ctx) throw new Error('useReviewDecision must be within ReviewDecisionProvider');
  return ctx;
}
```

- [ ] **Step 2: Create the research layout**

```tsx
// frontend/src/app/admin/research/layout.tsx
import { ReviewDecisionProvider } from '@/lib/research/ReviewDecisionContext';

export default function ResearchLayout({ children }: { children: React.ReactNode }) {
  return <ReviewDecisionProvider>{children}</ReviewDecisionProvider>;
}
```

- [ ] **Step 3: Commit**

```bash
git add frontend/src/lib/research/ReviewDecisionContext.tsx frontend/src/app/admin/research/layout.tsx
git commit -m "feat(research-ui): ReviewDecisionContext + research layout wrapper"
```

---

## Task 8: ResearchStatChip + ReadinessBadge

**Files:**
- Create: `frontend/src/components/research/ResearchStatChip.tsx`
- Create: `frontend/src/components/research/ReadinessBadge.tsx`
- Create: `frontend/src/__tests__/components/research/ReadinessBadge.test.tsx`

- [ ] **Step 1: Write failing tests**

```tsx
// frontend/src/__tests__/components/research/ReadinessBadge.test.tsx
import { render, screen } from '@testing-library/react';
import { ReadinessBadge } from '@/components/research/ReadinessBadge';

describe('ReadinessBadge', () => {
  it('renders "Blocked" for blocked readiness', () => {
    render(<ReadinessBadge readiness="blocked" />);
    expect(screen.getByText('Blocked')).toBeInTheDocument();
  });

  it('renders "Review Required" for review-required', () => {
    render(<ReadinessBadge readiness="review-required" />);
    expect(screen.getByText('Review Required')).toBeInTheDocument();
  });

  it('renders "Candidate" for candidate-for-promotion', () => {
    render(<ReadinessBadge readiness="candidate-for-promotion" />);
    expect(screen.getByText('Candidate')).toBeInTheDocument();
  });

  it('applies red styling for blocked', () => {
    const { container } = render(<ReadinessBadge readiness="blocked" />);
    expect(container.firstChild).toHaveClass('text-rose-400');
  });

  it('applies amber styling for review-required', () => {
    const { container } = render(<ReadinessBadge readiness="review-required" />);
    expect(container.firstChild).toHaveClass('text-amber-400');
  });

  it('applies green styling for candidate', () => {
    const { container } = render(<ReadinessBadge readiness="candidate-for-promotion" />);
    expect(container.firstChild).toHaveClass('text-emerald-400');
  });
});
```

- [ ] **Step 2: Run — verify FAIL**

```bash
cd frontend && npx vitest run src/__tests__/components/research/ReadinessBadge.test.tsx
```

- [ ] **Step 3: Implement both components**

```tsx
// frontend/src/components/research/ResearchStatChip.tsx
interface ResearchStatChipProps {
  label: string;
  value: number | string;
  color: 'red' | 'amber' | 'green' | 'blue' | 'neutral';
}

const colorMap = {
  red:     'text-rose-400 bg-rose-400/5 border-rose-400/20',
  amber:   'text-amber-400 bg-amber-400/5 border-amber-400/20',
  green:   'text-emerald-400 bg-emerald-400/5 border-emerald-400/20',
  blue:    'text-blue-400 bg-blue-400/5 border-blue-400/20',
  neutral: 'text-white/50 bg-white/[0.03] border-white/10',
};

export function ResearchStatChip({ label, value, color }: ResearchStatChipProps) {
  return (
    <div className={`rounded-2xl border p-4 ${colorMap[color]}`}>
      <p className="text-[10px] uppercase font-bold tracking-widest opacity-60">{label}</p>
      <p className="text-2xl font-bold mt-1 text-white">{value}</p>
    </div>
  );
}
```

```tsx
// frontend/src/components/research/ReadinessBadge.tsx
import type { PromotionReadiness } from '@/lib/research/types';

interface ReadinessBadgeProps { readiness: PromotionReadiness | string }

const config: Record<string, { label: string; classes: string }> = {
  'blocked':                 { label: 'Blocked',          classes: 'bg-rose-500/15 text-rose-400 border-rose-400/20' },
  'review-required':         { label: 'Review Required',  classes: 'bg-amber-500/15 text-amber-400 border-amber-400/20' },
  'candidate-for-promotion': { label: 'Candidate',        classes: 'bg-emerald-500/15 text-emerald-400 border-emerald-400/20' },
};

export function ReadinessBadge({ readiness }: ReadinessBadgeProps) {
  const { label, classes } = config[readiness] ?? { label: readiness, classes: 'bg-white/10 text-white/50 border-white/15' };
  return (
    <span className={`text-[9px] font-bold tracking-widest uppercase px-2 py-1 rounded-full border ${classes}`}>
      {label}
    </span>
  );
}
```

- [ ] **Step 4: Run — verify PASS**

```bash
cd frontend && npx vitest run src/__tests__/components/research/ReadinessBadge.test.tsx
```

- [ ] **Step 5: Commit**

```bash
git add frontend/src/components/research/ResearchStatChip.tsx frontend/src/components/research/ReadinessBadge.tsx frontend/src/__tests__/components/research/ReadinessBadge.test.tsx
git commit -m "feat(research-ui): ResearchStatChip + ReadinessBadge components"
```

---

## Task 9: BlockerCard

**Files:**
- Create: `frontend/src/components/research/BlockerCard.tsx`
- Create: `frontend/src/__tests__/components/research/BlockerCard.test.tsx`

- [ ] **Step 1: Write failing tests**

```tsx
// frontend/src/__tests__/components/research/BlockerCard.test.tsx
import { render, screen } from '@testing-library/react';
import { BlockerCard } from '@/components/research/BlockerCard';

describe('BlockerCard', () => {
  it('renders the blocker text', () => {
    render(<BlockerCard blocker="blocked: missing required authoritative support" />);
    expect(screen.getByText(/missing required authoritative support/)).toBeInTheDocument();
  });

  it('applies red styling for blocked: prefix', () => {
    const { container } = render(<BlockerCard blocker="blocked: some issue" />);
    expect(container.firstChild).toHaveClass('border-rose-400/25');
  });

  it('applies amber styling for review-required: prefix', () => {
    const { container } = render(<BlockerCard blocker="review-required: needs review" />);
    expect(container.firstChild).toHaveClass('border-amber-400/25');
  });

  it('shows ✕ icon for hard blockers', () => {
    render(<BlockerCard blocker="blocked: something" />);
    expect(screen.getByText('✕')).toBeInTheDocument();
  });

  it('shows ⚠ icon for review-required', () => {
    render(<BlockerCard blocker="review-required: something" />);
    expect(screen.getByText('⚠')).toBeInTheDocument();
  });
});
```

- [ ] **Step 2: Run — verify FAIL**

```bash
cd frontend && npx vitest run src/__tests__/components/research/BlockerCard.test.tsx
```

- [ ] **Step 3: Implement**

```tsx
// frontend/src/components/research/BlockerCard.tsx
interface BlockerCardProps { blocker: string }

export function BlockerCard({ blocker }: BlockerCardProps) {
  const isHard = blocker.startsWith('blocked:');
  return (
    <div className={`flex gap-3 items-start rounded-xl border px-3 py-2.5 ${
      isHard
        ? 'bg-rose-500/8 border-rose-400/25'
        : 'bg-amber-500/8 border-amber-400/25'
    }`}>
      <span className={`text-xs mt-0.5 flex-shrink-0 font-bold ${isHard ? 'text-rose-400' : 'text-amber-400'}`}>
        {isHard ? '✕' : '⚠'}
      </span>
      <span className={`text-xs leading-relaxed ${isHard ? 'text-rose-300' : 'text-amber-300'}`}>
        {blocker}
      </span>
    </div>
  );
}
```

- [ ] **Step 4: Run — verify PASS**

```bash
cd frontend && npx vitest run src/__tests__/components/research/BlockerCard.test.tsx
```

- [ ] **Step 5: Commit**

```bash
git add frontend/src/components/research/BlockerCard.tsx frontend/src/__tests__/components/research/BlockerCard.test.tsx
git commit -m "feat(research-ui): BlockerCard — hard/soft blocker distinction"
```

---

## Task 10: CompoundCard

**Files:**
- Create: `frontend/src/components/research/CompoundCard.tsx`
- Create: `frontend/src/__tests__/components/research/CompoundCard.test.tsx`

- [ ] **Step 1: Write failing tests**

```tsx
// frontend/src/__tests__/components/research/CompoundCard.test.tsx
import { render, screen } from '@testing-library/react';
import { CompoundCard } from '@/components/research/CompoundCard';
import type { ResearchSummaryCompound } from '@/lib/research/types';

const blocked: ResearchSummaryCompound = {
  name: 'BPC-157', classification: 'Peptide', overallEvidenceTier: 'Limited',
  completeness: 'partial', needsReview: true, reviewQueueItemCount: 3,
  promotionReadiness: 'blocked',
  promotionBlockers: ['blocked: missing required authoritative support'],
  reviewDecisionIds: [], qualityFlags: [], reviewReasons: [],
};

const candidate: ResearchSummaryCompound = {
  name: 'Creatine', classification: 'Supplement', overallEvidenceTier: 'Strong',
  completeness: 'substantial', needsReview: false, reviewQueueItemCount: 0,
  promotionReadiness: 'candidate-for-promotion',
  promotionBlockers: [], reviewDecisionIds: [], qualityFlags: [], reviewReasons: [],
};

describe('CompoundCard', () => {
  it('renders compound name', () => {
    render(<CompoundCard compound={blocked} selected={false} onClick={() => {}} />);
    expect(screen.getByText('BPC-157')).toBeInTheDocument();
  });

  it('shows ReadinessBadge text', () => {
    render(<CompoundCard compound={blocked} selected={false} onClick={() => {}} />);
    expect(screen.getByText('Blocked')).toBeInTheDocument();
  });

  it('shows first blocker for blocked compounds', () => {
    render(<CompoundCard compound={blocked} selected={false} onClick={() => {}} />);
    expect(screen.getByText(/missing required authoritative support/)).toBeInTheDocument();
  });

  it('does not show blocker text for candidates', () => {
    render(<CompoundCard compound={candidate} selected={false} onClick={() => {}} />);
    expect(screen.queryByText(/blocked:/)).not.toBeInTheDocument();
  });

  it('shows metadata: classification, tier, completeness, queue count', () => {
    render(<CompoundCard compound={blocked} selected={false} onClick={() => {}} />);
    expect(screen.getByText('Peptide')).toBeInTheDocument();
    expect(screen.getByText('Limited')).toBeInTheDocument();
    expect(screen.getByText('partial')).toBeInTheDocument();
    expect(screen.getByText('3')).toBeInTheDocument();
  });

  it('calls onClick when clicked', () => {
    const onClick = vi.fn();
    render(<CompoundCard compound={blocked} selected={false} onClick={onClick} />);
    screen.getByText('BPC-157').closest('[role="button"]')?.click();
    // or: userEvent.click(screen.getByText('BPC-157'));
    expect(onClick).toHaveBeenCalled();
  });
});
```

- [ ] **Step 2: Run — verify FAIL**

```bash
cd frontend && npx vitest run src/__tests__/components/research/CompoundCard.test.tsx
```

- [ ] **Step 3: Implement**

```tsx
// frontend/src/components/research/CompoundCard.tsx
import { cn } from '@/lib/utils';
import { ReadinessBadge } from './ReadinessBadge';
import type { ResearchSummaryCompound } from '@/lib/research/types';

interface CompoundCardProps {
  compound: ResearchSummaryCompound;
  selected: boolean;
  onClick: () => void;
}

const borderColor: Record<string, string> = {
  'blocked':                 'border-l-rose-500',
  'review-required':         'border-l-amber-500',
  'candidate-for-promotion': 'border-l-emerald-500',
};

export function CompoundCard({ compound, selected, onClick }: CompoundCardProps) {
  const firstBlocker = compound.promotionBlockers[0];
  const showBlocker = firstBlocker && compound.promotionReadiness !== 'candidate-for-promotion';

  return (
    <div
      role="button"
      tabIndex={0}
      onClick={onClick}
      onKeyDown={e => e.key === 'Enter' && onClick()}
      className={cn(
        'rounded-xl border-l-4 border border-white/10 px-3 py-2.5 cursor-pointer transition-all',
        borderColor[compound.promotionReadiness] ?? 'border-l-white/20',
        selected ? 'bg-blue-900/30 border-white/20' : 'bg-white/[0.03] hover:bg-white/[0.06]'
      )}
    >
      <div className="flex items-start justify-between gap-2 mb-1.5">
        <span className="text-[13px] font-semibold text-white leading-tight">{compound.name}</span>
        <ReadinessBadge readiness={compound.promotionReadiness} />
      </div>
      <div className="flex flex-wrap gap-x-3 gap-y-1">
        <MetaItem label="Class" value={compound.classification} />
        <MetaItem label="Tier" value={compound.overallEvidenceTier} />
        <MetaItem label="Complete" value={compound.completeness} />
        {compound.reviewQueueItemCount > 0 && (
          <MetaItem label="Queue" value={String(compound.reviewQueueItemCount)} />
        )}
      </div>
      {showBlocker && (
        <p className="mt-1.5 text-[10px] text-rose-400/70 italic truncate">{firstBlocker}</p>
      )}
    </div>
  );
}

function MetaItem({ label, value }: { label: string; value: string }) {
  return (
    <span className="text-[11px] text-white/40">
      {label}: <span className="text-white/70">{value}</span>
    </span>
  );
}
```

- [ ] **Step 4: Run — verify PASS**

```bash
cd frontend && npx vitest run src/__tests__/components/research/CompoundCard.test.tsx
```

- [ ] **Step 5: Commit**

```bash
git add frontend/src/components/research/CompoundCard.tsx frontend/src/__tests__/components/research/CompoundCard.test.tsx
git commit -m "feat(research-ui): CompoundCard with readiness border, metadata, blocker preview"
```

---

## Task 11: FilterBar

**Files:**
- Create: `frontend/src/components/research/FilterBar.tsx`
- Create: `frontend/src/__tests__/components/research/FilterBar.test.tsx`

- [ ] **Step 1: Write failing tests**

```tsx
// frontend/src/__tests__/components/research/FilterBar.test.tsx
import { render, screen, fireEvent } from '@testing-library/react';
import { FilterBar } from '@/components/research/FilterBar';
import type { ResearchReviewCategory } from '@/lib/research/types';

vi.mock('next/navigation', () => ({
  useRouter: () => ({ replace: vi.fn() }),
  useSearchParams: () => new URLSearchParams(),
  usePathname: () => '/admin/research/compounds',
}));

const categories: ResearchReviewCategory[] = [
  { name: 'Safety Critical', count: 2, compounds: [], signals: [], recommendedActions: [] },
  { name: 'Regulatory / Approval', count: 1, compounds: [], signals: [], recommendedActions: [] },
];

describe('FilterBar', () => {
  it('renders readiness chips with counts', () => {
    render(<FilterBar blockedCount={3} reviewCount={7} candidateCount={4} categories={categories} />);
    expect(screen.getByText('Blocked (3)')).toBeInTheDocument();
    expect(screen.getByText('Review Required (7)')).toBeInTheDocument();
    expect(screen.getByText('Candidate (4)')).toBeInTheDocument();
  });

  it('renders review category chips', () => {
    render(<FilterBar blockedCount={1} reviewCount={1} candidateCount={1} categories={categories} />);
    expect(screen.getByText('Safety Critical')).toBeInTheDocument();
    expect(screen.getByText('Regulatory / Approval')).toBeInTheDocument();
  });

  it('"More filters" expander is collapsed by default — Strong tier hidden', () => {
    render(<FilterBar blockedCount={1} reviewCount={1} candidateCount={1} categories={categories} />);
    expect(screen.queryByText('Strong')).not.toBeInTheDocument();
  });

  it('clicking "More filters" reveals evidence tier chips', () => {
    render(<FilterBar blockedCount={1} reviewCount={1} candidateCount={1} categories={categories} />);
    fireEvent.click(screen.getByText('More filters'));
    expect(screen.getByText('Strong')).toBeInTheDocument();
  });
});
```

- [ ] **Step 2: Run — verify FAIL**

```bash
cd frontend && npx vitest run src/__tests__/components/research/FilterBar.test.tsx
```

- [ ] **Step 3: Implement**

```tsx
// frontend/src/components/research/FilterBar.tsx
'use client';
import { useState } from 'react';
import { useRouter, useSearchParams, usePathname } from 'next/navigation';
import { cn } from '@/lib/utils';
import type { ResearchReviewCategory } from '@/lib/research/types';

interface FilterBarProps {
  blockedCount: number;
  reviewCount: number;
  candidateCount: number;
  categories: ResearchReviewCategory[];
}

const EVIDENCE_TIERS = ['Strong', 'Moderate', 'Limited', 'Insufficient', 'Unknown', 'Anecdotal'];
const SORT_OPTIONS = [
  { value: 'risk', label: 'Risk Priority' },
  { value: 'name', label: 'Compound Name' },
  { value: 'tier', label: 'Evidence Tier' },
  { value: 'completeness', label: 'Completeness' },
];

export function FilterBar({ blockedCount, reviewCount, candidateCount, categories }: FilterBarProps) {
  const router = useRouter();
  const searchParams = useSearchParams();
  const pathname = usePathname();
  const [moreOpen, setMoreOpen] = useState(false);

  const activeReadiness = searchParams.getAll('readiness');
  const activeCategories = searchParams.getAll('category');
  const activeTiers = searchParams.getAll('tier');
  const activeSort = searchParams.get('sort') ?? 'risk';

  function toggleMulti(key: string, value: string) {
    const params = new URLSearchParams(searchParams.toString());
    const current = params.getAll(key);
    params.delete(key);
    if (current.includes(value)) {
      current.filter(v => v !== value).forEach(v => params.append(key, v));
    } else {
      [...current, value].forEach(v => params.append(key, v));
    }
    router.replace(`${pathname}?${params.toString()}`);
  }

  function setSort(value: string) {
    const params = new URLSearchParams(searchParams.toString());
    params.set('sort', value);
    router.replace(`${pathname}?${params.toString()}`);
  }

  return (
    <div className="rounded-2xl border border-white/10 bg-white/[0.03] p-3 flex flex-col gap-3">
      <div>
        <p className="text-[9px] font-bold uppercase tracking-widest text-white/30 mb-2">Readiness</p>
        <div className="flex flex-wrap gap-1.5">
          {[
            { key: 'blocked',                 label: `Blocked (${blockedCount})`,        cls: 'text-rose-400 border-rose-400/30 bg-rose-400/10' },
            { key: 'review-required',         label: `Review Required (${reviewCount})`, cls: 'text-amber-400 border-amber-400/30 bg-amber-400/10' },
            { key: 'candidate-for-promotion', label: `Candidate (${candidateCount})`,    cls: 'text-emerald-400 border-emerald-400/30 bg-emerald-400/10' },
          ].map(chip => (
            <button key={chip.key} onClick={() => toggleMulti('readiness', chip.key)}
              className={cn('text-[10px] px-2.5 py-1 rounded-full border transition-all',
                activeReadiness.includes(chip.key) ? chip.cls : 'text-white/40 border-white/15 bg-white/[0.03] hover:border-white/25')}>
              {chip.label}
            </button>
          ))}
        </div>
      </div>

      <div>
        <p className="text-[9px] font-bold uppercase tracking-widest text-white/30 mb-2">Review Category</p>
        <div className="flex flex-wrap gap-1.5">
          {categories.map(cat => (
            <button key={cat.name} onClick={() => toggleMulti('category', cat.name)}
              className={cn('text-[10px] px-2.5 py-1 rounded-full border transition-all',
                activeCategories.includes(cat.name)
                  ? 'text-white border-white/40 bg-white/15'
                  : 'text-white/40 border-white/15 bg-white/[0.03] hover:border-white/25')}>
              {cat.name}
            </button>
          ))}
        </div>
      </div>

      <button onClick={() => setMoreOpen(v => !v)}
        className="text-[10px] text-white/30 hover:text-white/60 text-left transition-colors">
        {moreOpen ? '▲ Fewer filters' : 'More filters'}
      </button>

      {moreOpen && (
        <div className="border-t border-white/[0.06] pt-2">
          <p className="text-[9px] font-bold uppercase tracking-widest text-white/30 mb-2">Evidence Tier</p>
          <div className="flex flex-wrap gap-1.5">
            {EVIDENCE_TIERS.map(tier => (
              <button key={tier} onClick={() => toggleMulti('tier', tier)}
                className={cn('text-[10px] px-2.5 py-1 rounded-full border transition-all',
                  activeTiers.includes(tier)
                    ? 'text-white border-white/40 bg-white/15'
                    : 'text-white/40 border-white/15 bg-white/[0.03] hover:border-white/25')}>
                {tier}
              </button>
            ))}
          </div>
        </div>
      )}

      <div className="flex items-center gap-2">
        <p className="text-[9px] font-bold uppercase tracking-widest text-white/30">Sort</p>
        <select value={activeSort} onChange={e => setSort(e.target.value)}
          className="bg-white/[0.05] border border-white/15 text-white/70 text-[10px] rounded-lg px-2 py-1">
          {SORT_OPTIONS.map(o => <option key={o.value} value={o.value}>{o.label}</option>)}
        </select>
      </div>
    </div>
  );
}
```

- [ ] **Step 4: Run — verify PASS**

```bash
cd frontend && npx vitest run src/__tests__/components/research/FilterBar.test.tsx
```

- [ ] **Step 5: Commit**

```bash
git add frontend/src/components/research/FilterBar.tsx frontend/src/__tests__/components/research/FilterBar.test.tsx
git commit -m "feat(research-ui): FilterBar with URL query param sync"
```

---

## Task 12: ClaimCard + ResolutionPlanItem

**Files:**
- Create: `frontend/src/components/research/ClaimCard.tsx`
- Create: `frontend/src/components/research/ResolutionPlanItem.tsx`
- Create: `frontend/src/__tests__/components/research/ClaimCard.test.tsx`
- Create: `frontend/src/__tests__/components/research/ResolutionPlanItem.test.tsx`

- [ ] **Step 1: Write failing tests for both**

```tsx
// frontend/src/__tests__/components/research/ClaimCard.test.tsx
import { render, screen, fireEvent } from '@testing-library/react';
import { ClaimCard } from '@/components/research/ClaimCard';
import type { EvidenceClaim } from '@/lib/research/types';

const claim: EvidenceClaim = {
  claimId: 'c-001', claimType: 'warning',
  statement: 'No human safety data exist for systemic use.',
  context: { population: 'General', route: null, formulation: null, useCase: 'Research', doseText: null },
  evidenceTier: 'Limited', confidence: 'low', fieldAuthorityRequired: true,
  sourceRefs: ['src-001'],
  extractedEvidence: [{ sourceRef: 'src-001', quote: 'No human trials.', pageOrSection: 'p.12' }],
  reviewFlags: ['needs-expert-review'],
};

describe('ClaimCard', () => {
  it('renders claim type uppercased', () => {
    render(<ClaimCard claim={claim} />);
    expect(screen.getByText('WARNING')).toBeInTheDocument();
  });

  it('renders the statement', () => {
    render(<ClaimCard claim={claim} />);
    expect(screen.getByText('No human safety data exist for systemic use.')).toBeInTheDocument();
  });

  it('shows Field Authority Required badge', () => {
    render(<ClaimCard claim={claim} />);
    expect(screen.getByText('Field Authority Required')).toBeInTheDocument();
  });

  it('shows — for null context fields', () => {
    render(<ClaimCard claim={claim} />);
    const dashes = screen.getAllByText('—');
    expect(dashes.length).toBeGreaterThan(0);
  });

  it('shows non-null context values', () => {
    render(<ClaimCard claim={claim} />);
    expect(screen.getByText('General')).toBeInTheDocument();
  });

  it('toggles extracted evidence on click', () => {
    render(<ClaimCard claim={claim} />);
    expect(screen.queryByText('No human trials.')).not.toBeInTheDocument();
    fireEvent.click(screen.getByText(/Show evidence/i));
    expect(screen.getByText('No human trials.')).toBeInTheDocument();
  });
});
```

```tsx
// frontend/src/__tests__/components/research/ResolutionPlanItem.test.tsx
import { render, screen } from '@testing-library/react';
import { ResolutionPlanItem } from '@/components/research/ResolutionPlanItem';
import type { ReviewResolutionPlanItem } from '@/lib/research/types';

const item: ReviewResolutionPlanItem = {
  itemId: 'bpc-157-resolution-1', compoundName: 'BPC-157',
  readiness: 'blocked', severity: 'blocked',
  resolutionType: 'add-authoritative-source',
  issue: 'blocked: missing required authoritative support',
  recommendedAction: 'Attach an A1/A2 source before promotion.',
  relatedBlockers: ['blocked: missing required authoritative support'],
  relatedQualityFlags: ['missing-authoritative-support'],
};

describe('ResolutionPlanItem', () => {
  it('renders resolution type', () => {
    render(<ResolutionPlanItem item={item} />);
    expect(screen.getByText('add-authoritative-source')).toBeInTheDocument();
  });

  it('renders the issue text', () => {
    render(<ResolutionPlanItem item={item} />);
    expect(screen.getByText(/missing required authoritative support/)).toBeInTheDocument();
  });

  it('renders the recommended action', () => {
    render(<ResolutionPlanItem item={item} />);
    expect(screen.getByText('Attach an A1/A2 source before promotion.')).toBeInTheDocument();
  });

  it('renders quality flags', () => {
    render(<ResolutionPlanItem item={item} />);
    expect(screen.getByText('missing-authoritative-support')).toBeInTheDocument();
  });
});
```

- [ ] **Step 2: Run — verify FAIL**

```bash
cd frontend && npx vitest run src/__tests__/components/research/ClaimCard.test.tsx src/__tests__/components/research/ResolutionPlanItem.test.tsx
```

- [ ] **Step 3: Implement ClaimCard**

```tsx
// frontend/src/components/research/ClaimCard.tsx
'use client';
import { useState } from 'react';
import { EvidenceTierBadge } from '@/components/knowledge/EvidenceTierBadge';
import type { EvidenceClaim } from '@/lib/research/types';

interface ClaimCardProps { claim: EvidenceClaim }

export function ClaimCard({ claim }: ClaimCardProps) {
  const [showEvidence, setShowEvidence] = useState(false);

  return (
    <div className="rounded-xl border border-white/[0.08] bg-white/[0.025] px-3 py-2.5 flex flex-col gap-2">
      <div className="flex items-center justify-between gap-2">
        <span className="text-[9px] font-bold tracking-widest uppercase text-white/40">{claim.claimType}</span>
        <EvidenceTierBadge tier={claim.evidenceTier} variant="research" />
      </div>
      <p className="text-[12px] text-white/90 leading-relaxed">{claim.statement}</p>
      <div className="grid grid-cols-2 gap-x-4 gap-y-0.5">
        {[
          { label: 'Population', value: claim.context.population },
          { label: 'Route',      value: claim.context.route },
          { label: 'Formulation',value: claim.context.formulation },
          { label: 'Use Case',   value: claim.context.useCase },
          { label: 'Dose',       value: claim.context.doseText },
        ].map(({ label, value }) => (
          <span key={label} className="text-[10px] text-white/35">
            {label}: <span className="text-white/60">{value ?? '—'}</span>
          </span>
        ))}
      </div>
      <div className="flex flex-wrap gap-1.5">
        {claim.fieldAuthorityRequired && (
          <span className="text-[9px] px-2 py-0.5 rounded-full bg-rose-500/15 border border-rose-400/25 text-rose-300">
            Field Authority Required
          </span>
        )}
        {claim.reviewFlags.map(flag => (
          <span key={flag} className="text-[9px] px-2 py-0.5 rounded-full bg-white/[0.05] border border-white/10 text-white/40">{flag}</span>
        ))}
      </div>
      {claim.extractedEvidence.length > 0 && (
        <div>
          <button onClick={() => setShowEvidence(v => !v)}
            className="text-[10px] text-white/30 hover:text-white/60 transition-colors">
            {showEvidence ? 'Hide evidence' : `Show evidence (${claim.extractedEvidence.length})`}
          </button>
          {showEvidence && (
            <div className="mt-2 flex flex-col gap-2">
              {claim.extractedEvidence.map((ev, i) => (
                <div key={i} className="rounded-lg bg-white/[0.03] border border-white/[0.06] px-2.5 py-2">
                  {ev.quote && <p className="text-[11px] text-white/70 italic">"{ev.quote}"</p>}
                  <p className="text-[10px] text-white/35 mt-1">{ev.sourceRef}{ev.pageOrSection && ` · ${ev.pageOrSection}`}</p>
                </div>
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  );
}
```

- [ ] **Step 4: Implement ResolutionPlanItem**

```tsx
// frontend/src/components/research/ResolutionPlanItem.tsx
import { ReadinessBadge } from './ReadinessBadge';
import type { ReviewResolutionPlanItem } from '@/lib/research/types';

export function ResolutionPlanItem({ item }: { item: ReviewResolutionPlanItem }) {
  return (
    <div className="rounded-xl border border-white/[0.08] bg-white/[0.025] px-3 py-2.5 flex flex-col gap-2">
      <div className="flex items-center justify-between gap-2">
        <span className="text-[10px] font-bold text-amber-400">{item.resolutionType}</span>
        <ReadinessBadge readiness={item.readiness as 'blocked' | 'review-required' | 'candidate-for-promotion'} />
      </div>
      <p className="text-[11px] text-white/70">{item.issue}</p>
      <div className="rounded-lg bg-blue-900/20 border border-blue-400/20 px-2.5 py-2">
        <p className="text-[11px] text-blue-300">→ {item.recommendedAction}</p>
      </div>
      {item.relatedQualityFlags.length > 0 && (
        <div className="flex flex-wrap gap-1.5">
          {item.relatedQualityFlags.map(f => (
            <span key={f} className="text-[9px] px-2 py-0.5 rounded-full bg-white/[0.05] border border-white/10 text-white/40">{f}</span>
          ))}
        </div>
      )}
    </div>
  );
}
```

- [ ] **Step 5: Run — verify PASS**

```bash
cd frontend && npx vitest run src/__tests__/components/research/ClaimCard.test.tsx src/__tests__/components/research/ResolutionPlanItem.test.tsx
```

- [ ] **Step 6: Commit**

```bash
git add frontend/src/components/research/ClaimCard.tsx frontend/src/components/research/ResolutionPlanItem.tsx frontend/src/__tests__/components/research/ClaimCard.test.tsx frontend/src/__tests__/components/research/ResolutionPlanItem.test.tsx
git commit -m "feat(research-ui): ClaimCard + ResolutionPlanItem"
```

---

## Task 13: ReviewDecisionForm

**Files:**
- Create: `frontend/src/components/research/ReviewDecisionForm.tsx`
- Create: `frontend/src/__tests__/components/research/ReviewDecisionForm.test.tsx`

- [ ] **Step 1: Write failing tests**

```tsx
// frontend/src/__tests__/components/research/ReviewDecisionForm.test.tsx
import { render, screen, fireEvent } from '@testing-library/react';
import { ReviewDecisionForm } from '@/components/research/ReviewDecisionForm';
import type { PromotionManifestCandidate } from '@/lib/research/types';

const mockContext = {
  batch: { schemaVersion: '1.0.0' as const, recordType: 'review-decision-batch' as const, batch: { batchId: 'b1', reviewerId: 'r1', reviewedAt: '', notes: [] }, decisions: [] },
  reviewerId: 'r1', setReviewerId: vi.fn(), addToSession: vi.fn(),
};
vi.mock('@/lib/research/ReviewDecisionContext', () => ({ useReviewDecision: () => mockContext }));

const blocked: PromotionManifestCandidate = {
  name: 'BPC-157', classification: 'Peptide', readiness: 'blocked',
  overallEvidenceTier: 'Limited', completeness: 'partial', reviewQueueItemCount: 3,
  reviewDecisionIds: [], blockers: ['blocked: missing required authoritative support'],
  qualityFlags: ['missing-authoritative-support'], requiredNextActions: [],
};

const reviewRequired: PromotionManifestCandidate = {
  name: 'Semaglutide', classification: 'Pharmaceutical', readiness: 'review-required',
  overallEvidenceTier: 'Strong', completeness: 'substantial', reviewQueueItemCount: 2,
  reviewDecisionIds: [], blockers: ['review-required: draft is marked needsReview'],
  qualityFlags: [], requiredNextActions: [],
};

describe('ReviewDecisionForm — blocked compound', () => {
  it('shows hard blocker guard banner', () => {
    render(<ReviewDecisionForm candidate={blocked} />);
    expect(screen.getByText(/unresolved hard blockers/i)).toBeInTheDocument();
  });

  it('does not show Add to Batch button', () => {
    render(<ReviewDecisionForm candidate={blocked} />);
    expect(screen.queryByText(/Add to Decision Batch/i)).not.toBeInTheDocument();
  });
});

describe('ReviewDecisionForm — review-required compound', () => {
  it('renders all decision radio options', () => {
    render(<ReviewDecisionForm candidate={reviewRequired} />);
    expect(screen.getByLabelText('approve-for-promotion')).toBeInTheDocument();
    expect(screen.getByLabelText('approve-claims')).toBeInTheDocument();
    expect(screen.getByLabelText('request-changes')).toBeInTheDocument();
    expect(screen.getByLabelText('reject')).toBeInTheDocument();
  });

  it('shows Add to Decision Batch button', () => {
    render(<ReviewDecisionForm candidate={reviewRequired} />);
    expect(screen.getByText(/Add to Decision Batch/i)).toBeInTheDocument();
  });

  it('blocks submit without selecting a decision', () => {
    render(<ReviewDecisionForm candidate={reviewRequired} />);
    fireEvent.click(screen.getByText(/Add to Decision Batch/i));
    expect(screen.getByText(/Select a decision/i)).toBeInTheDocument();
  });

  it('requires notes when approve-for-promotion selected with soft blockers remaining', () => {
    render(<ReviewDecisionForm candidate={reviewRequired} />);
    fireEvent.click(screen.getByLabelText('approve-for-promotion'));
    fireEvent.click(screen.getByText(/Add to Decision Batch/i));
    expect(screen.getByText(/Notes required/i)).toBeInTheDocument();
  });
});
```

- [ ] **Step 2: Run — verify FAIL**

```bash
cd frontend && npx vitest run src/__tests__/components/research/ReviewDecisionForm.test.tsx
```

- [ ] **Step 3: Implement**

```tsx
// frontend/src/components/research/ReviewDecisionForm.tsx
'use client';
import { useState } from 'react';
import { useReviewDecision } from '@/lib/research/ReviewDecisionContext';
import { toJson } from '@/lib/research/reviewDecisionBatch';
import { BlockerCard } from './BlockerCard';
import type { PromotionManifestCandidate, ReviewDecisionType } from '@/lib/research/types';

const DECISIONS: { value: ReviewDecisionType; label: string }[] = [
  { value: 'approve-for-promotion', label: 'approve-for-promotion — clear all blockers, mark candidate' },
  { value: 'approve-claims',        label: 'approve-claims — approve specific claims only' },
  { value: 'request-changes',       label: 'request-changes — flag issues, send back for revision' },
  { value: 'reject',                label: 'reject — remove from promotion pipeline' },
];

export function ReviewDecisionForm({ candidate }: { candidate: PromotionManifestCandidate }) {
  const { batch, reviewerId, setReviewerId, addToSession } = useReviewDecision();
  const hardBlockers = candidate.blockers.filter(b => b.startsWith('blocked:'));
  const softBlockers = candidate.blockers.filter(b => b.startsWith('review-required:'));

  const [decision, setDecision] = useState<ReviewDecisionType | ''>('');
  const [notes, setNotes] = useState('');
  const [clearsSoft, setClearsSoft] = useState<boolean | null>(null);
  const [error, setError] = useState('');
  const [submitted, setSubmitted] = useState(false);

  if (hardBlockers.length > 0) {
    return (
      <div className="flex flex-col gap-3">
        <div className="rounded-xl bg-rose-500/10 border border-rose-400/25 px-4 py-3">
          <p className="text-sm font-semibold text-rose-300 mb-3">
            This compound has unresolved hard blockers. Resolve all <code>blocked:</code> items before submitting a review decision.
          </p>
          {hardBlockers.map(b => <BlockerCard key={b} blocker={b} />)}
        </div>
      </div>
    );
  }

  function handleSubmit() {
    setError('');
    if (!decision) { setError('Select a decision.'); return; }
    if (!reviewerId.trim()) { setError('Reviewer ID is required.'); return; }
    const needsClearNote = decision === 'approve-for-promotion' && softBlockers.length > 0;
    if (needsClearNote && !notes.trim()) {
      setError('Notes required — explain why soft blockers are cleared when approving for promotion.');
      return;
    }
    if (needsClearNote && clearsSoft !== true) {
      setError('Set "Clears Soft Blockers" to Yes when approving with remaining soft blockers.');
      return;
    }
    addToSession({
      decisionId: `dec-${Date.now()}`,
      compoundName: candidate.name,
      decision,
      reviewerId,
      reviewedAt: new Date().toISOString(),
      scope: { claimIds: [], qualityFlags: candidate.qualityFlags, reviewCategories: [], promotionBlockers: candidate.blockers },
      clearsSoftPromotionBlockers: clearsSoft ?? false,
      expiresAt: null,
      notes: notes.trim() ? [notes.trim()] : [],
    });
    setSubmitted(true);
  }

  if (submitted) {
    return (
      <div className="rounded-xl bg-emerald-500/10 border border-emerald-400/25 px-4 py-3">
        <p className="text-sm text-emerald-300">Decision added to batch ({batch.decisions.length + 1} total).</p>
        <button onClick={() => { setSubmitted(false); setDecision(''); setNotes(''); setClearsSoft(null); }}
          className="text-[11px] text-white/40 mt-2 hover:text-white/70">Add another decision</button>
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-4">
      {softBlockers.map(b => <BlockerCard key={b} blocker={b} />)}

      <div>
        <p className="text-[9px] font-bold uppercase tracking-widest text-white/30 mb-2">Decision</p>
        <div className="flex flex-col gap-2">
          {DECISIONS.map(d => (
            <label key={d.value} className={`flex items-center gap-2.5 rounded-xl border px-3 py-2 cursor-pointer transition-all ${decision === d.value ? 'border-emerald-400/40 bg-emerald-500/8' : 'border-white/10 hover:border-white/20'}`}>
              <input type="radio" aria-label={d.value} name="decision" value={d.value} checked={decision === d.value} onChange={() => setDecision(d.value)} className="accent-emerald-500" />
              <span className="text-[11px] text-white/80">{d.label}</span>
            </label>
          ))}
        </div>
      </div>

      <div>
        <p className="text-[9px] font-bold uppercase tracking-widest text-white/30 mb-1.5">Reviewer ID</p>
        <input type="text" value={reviewerId} onChange={e => setReviewerId(e.target.value)} placeholder="your-reviewer-id"
          className="w-full bg-white/[0.05] border border-white/15 rounded-xl px-3 py-2 text-[12px] text-white placeholder-white/25 focus:outline-none focus:border-white/30" />
      </div>

      <div>
        <p className="text-[9px] font-bold uppercase tracking-widest text-white/30 mb-1.5">
          Notes {decision === 'approve-for-promotion' && softBlockers.length > 0 && <span className="text-rose-400">*</span>}
        </p>
        <textarea value={notes} onChange={e => setNotes(e.target.value)} rows={3} placeholder="Add notes for this decision..."
          className="w-full bg-white/[0.05] border border-white/15 rounded-xl px-3 py-2 text-[12px] text-white placeholder-white/25 focus:outline-none focus:border-white/30 resize-none" />
      </div>

      {softBlockers.length > 0 && (
        <div>
          <p className="text-[9px] font-bold uppercase tracking-widest text-white/30 mb-2">
            Clears Soft Promotion Blockers {decision === 'approve-for-promotion' && <span className="text-rose-400">*</span>}
          </p>
          <div className="flex gap-3">
            {([true, false] as const).map(v => (
              <label key={String(v)} className={`flex items-center gap-2 rounded-xl border px-3 py-2 cursor-pointer transition-all text-[11px] ${clearsSoft === v ? 'border-emerald-400/40 bg-emerald-500/8 text-emerald-300' : 'border-white/10 text-white/50 hover:border-white/20'}`}>
                <input type="radio" name="clearsSoft" checked={clearsSoft === v} onChange={() => setClearsSoft(v)} className="accent-emerald-500" />
                {v ? 'Yes' : 'No'}
              </label>
            ))}
          </div>
        </div>
      )}

      {error && <p className="text-[11px] text-rose-300 bg-rose-500/10 border border-rose-400/20 rounded-lg px-3 py-2">{error}</p>}

      <button onClick={handleSubmit} className="bg-emerald-600 hover:bg-emerald-500 text-white text-[12px] font-semibold py-2.5 rounded-xl transition-colors">
        Add to Decision Batch
      </button>

      <div className="text-center text-[10px] text-white/30">
        {batch.decisions.length} decision(s) in batch ·{' '}
        <button onClick={() => {
          const blob = new Blob([toJson(batch)], { type: 'application/json' });
          const a = document.createElement('a');
          a.href = URL.createObjectURL(blob);
          a.download = 'review-decision-batch.json';
          a.click();
        }} className="hover:text-white/60 underline">Export batch as JSON</button>
      </div>
    </div>
  );
}
```

- [ ] **Step 4: Run — verify PASS**

```bash
cd frontend && npx vitest run src/__tests__/components/research/ReviewDecisionForm.test.tsx
```

- [ ] **Step 5: Commit**

```bash
git add frontend/src/components/research/ReviewDecisionForm.tsx frontend/src/__tests__/components/research/ReviewDecisionForm.test.tsx
git commit -m "feat(research-ui): ReviewDecisionForm — hard blocker guard, soft-blocker approval gate"
```

---

## Tasks 14–17: Pages

**Implement the four pages.** The full page implementations are specified in Tasks 14–17 of the original plan outline above (Dashboard, CompoundList, CompoundDetail, PipelinePage). Each follows this structure:

- [ ] Write test mock + render assertion
- [ ] Run — verify FAIL
- [ ] Implement page (`'use client'`, `useEffect` + `acquireToken` pattern matching `/admin/page.tsx`, `Header` + `GlassCard` layout)
- [ ] Run — verify PASS
- [ ] Commit

**Dashboard** (`/admin/research/page.tsx`): Stat bar (6 `ResearchStatChip`), left column (review categories list linking to `/admin/research/compounds?category=<name>`), right column (readiness bar chart, resolution plan summary).

**CompoundList** (`/admin/research/compounds/page.tsx`): `FilterBar` + scrollable list of `CompoundCard`s. Filter by readiness/category/tier from query params. Sort with `sortCompounds()`. Click navigates to `/admin/research/compounds/[slug]`.

**CompoundDetail** (`/admin/research/compounds/[slug]/page.tsx`): Resolve slug via `buildSlugMap`. Unknown slug → "not found" state with back link, no crash. Four tabs: Overview (status grid + `BlockerCard`s + required actions + quality flags), Claims (evidence packet per-compound, stub in v1), Resolution Plan (`ResolutionPlanItem`s filtered to this compound), Review Decision (`ReviewDecisionForm`).

**PipelinePage** (`/admin/research/pipeline/page.tsx`): Four expandable `GlassCard` sections. Use `Promise.allSettled` so one missing artifact doesn't break the whole page. Each missing artifact shows `<p>Artifact not yet generated for this run: {filename}</p>`.

---

## Task 18: Full Test Suite + Regression Check

- [ ] **Step 1: Run all research tests**

```bash
cd frontend && npx vitest run src/__tests__/lib/research/ src/__tests__/components/research/
```

Expected: All pass.

- [ ] **Step 2: Run full suite — check for regressions**

```bash
cd frontend && npm test
```

Expected: No pre-existing test failures. If `EvidenceTierBadge` or `CompoundIntelligenceCard` tests fail, the variant extension was not backward-compatible — check Task 6 Step 2.

- [ ] **Step 3: Verify `.superpowers/` in `.gitignore`**

```bash
grep -c '.superpowers' /d/Repos/BioStack/.gitignore || echo "MISSING — add .superpowers/ to .gitignore"
```

If missing, add `.superpowers/` to `.gitignore` and commit.

- [ ] **Step 4: Final commit**

```bash
git add .
git commit -m "feat(research-ui): research review workbench complete — dashboard, compound list, detail, pipeline"
```

---

## Spec Coverage Checklist

| Spec Requirement | Task |
|---|---|
| Flat route tree under `/admin/research` | 14–17 |
| `[slug]` routing + `slugToCanonicalName` lookup | 2, 15, 16 |
| Unknown slug → "not found" state, no crash | 16 |
| Missing artifact → section-level empty state | 14–17 |
| Fixtures + API route, env-switched (`RESEARCH_DATA_SOURCE`) | 4, 5 |
| API route: Bearer guard, fail closed, fixed allowlist, no path traversal | 5 |
| `getEvidenceTierColor` handles Insufficient/Unknown/Anecdotal | 6 |
| `EvidenceTierBadge` research variant — backward-compatible | 6 |
| Sidebar Research link + Admin exact-match fix | 6 |
| `ReviewDecisionContext` shared across all research pages | 7 |
| Stat bar (6 chips) + two-column dashboard body | 14 |
| Review category links to pre-filtered compound list | 14 |
| Filter bar: primary (readiness + category) + secondary expander | 11 |
| "More filters" collapsed by default | 11 |
| Filter/sort state synced to URL query params | 11 |
| Compound cards: left-border color, metadata row, first blocker preview | 10 |
| 4-tab compound detail | 16 |
| Overview: status grid, blockers, next actions, quality flags, categories | 16 |
| `Unknown` for semantic missing values; `—` for null context fields | 12 |
| Hard blocker guard: form disabled + banner | 13 |
| `approve-for-promotion` + soft blockers requires `clearsSoftPromotionBlockers=yes` + notes | 13 |
| Batch accumulates decisions; exports JSON | 3, 13 |
| Pipeline Panel: 4 expandable sections + empty states | 17 |
| Tests: filter, sort, badge colors, blocker distinction, form guard | 8–13 |
