// frontend/src/lib/research/types.ts

export type PromotionReadiness =
  | 'research-requested'
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
  | 'resolve-review-items' | 'archive-draft'
  | 'request-changes' | 'reject';

// ── Research Summary ──────────────────────────────────────────────────────────
export interface ResearchSummary {
  draftSubstanceCount: number;
  reviewQueueItemCount: number;
  researchRequestCount?: number;
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
  hasRequestedChanges?: boolean;
  hasResearchRequest?: boolean;
  researchRequestIds?: string[];
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
  outputs?: Record<string, string>;
  blocked: PromotionManifestCandidate[];
  reviewRequired: PromotionManifestCandidate[];
  researchRequested?: PromotionManifestCandidate[];
  candidatesForPromotion: PromotionManifestCandidate[];
}

export interface PromotionManifestCounts {
  totalDrafts: number;
  blocked: number;
  reviewRequired: number;
  researchRequested?: number;
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
  hasRequestedChanges?: boolean;
  hasResearchRequest?: boolean;
  researchRequestIds?: string[];
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
  relatedReviewQueueItemIds: string[];
  relatedBlockers: string[];
  relatedQualityFlags: string[];
}

export interface ResearchReviewQueueItem {
  itemId: string;
  compoundName: string;
  severity: string;
  reason: string;
  references: string[];
}

export interface ResearchRequestBatch {
  schemaVersion: '1.0.0';
  recordType: 'research-request-batch';
  batch: {
    batchId: string;
    requesterId: string;
    requestedAt: string;
    notes: string[];
  };
  requests: ResearchRequest[];
}

export interface ResearchRequest {
  requestId: string;
  compoundName: string;
  aliases: string[];
  classification: string;
  priority: 'low' | 'normal' | 'high' | 'urgent';
  requesterId: string;
  requestedAt: string;
  rationale: string;
  notes: string[];
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

// ── Promotion Export / Dry-Run ───────────────────────────────────────────────
export interface PromotionExportManifest {
  manifestVersion: string;
  generatedAtUtc: string;
  exportedCount: number;
  candidates: PromotionExportCandidate[];
  skippedCompounds: string[];
}

export interface PromotionExportCandidate {
  name: string;
  slug: string;
  readiness: PromotionReadiness;
  substanceFile: string;
  aggregateIndex: number;
  reviewDecisionIds: string[];
  qualityFlags: string[];
}

export interface PromotionImportDryRunReport {
  reportVersion: string;
  generatedAtUtc: string;
  previewPath: string;
  aggregatePath: string;
  safeToApply: boolean;
  refusalReasons: string[];
  previewCounts: PromotionImportPreview['counts'];
  items: PromotionImportDryRunItem[];
}

export interface PromotionImportDryRunItem {
  name: string;
  slug: string;
  plannedAction: 'create' | 'update';
  schemaValid: boolean;
  reasons: string[];
}

// ── Review Decision ───────────────────────────────────────────────────────────
export interface ReviewDecisionScope {
  claimIds: string[];
  reviewQueueItemIds: string[];
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
