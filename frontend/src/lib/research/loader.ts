import type {
    EvidencePacket,
    PromotionExportManifest,
    PromotionImportDryRunReport,
    PromotionImportPreview,
    PromotionManifest,
    ResearchReviewQueueItem,
    ResearchSummary,
    ReviewResolutionPlan,
} from './types';

type JsonLike = null | boolean | number | string | JsonLike[] | { [key: string]: JsonLike };

function camelizeKey(key: string): string {
  return key.length === 0 ? key : key[0].toLowerCase() + key.slice(1);
}

export function normalizeResearchArtifact<T>(artifact: JsonLike): T {
  if (Array.isArray(artifact)) {
    return artifact.map(item => normalizeResearchArtifact<JsonLike>(item)) as T;
  }

  if (artifact && typeof artifact === 'object') {
    return Object.fromEntries(
      Object.entries(artifact).map(([key, value]) => [
        camelizeKey(key),
        normalizeResearchArtifact<JsonLike>(value),
      ])
    ) as T;
  }

  return artifact as T;
}

async function fetchArtifact<T>(artifact: string, token: string): Promise<T> {
  const res = await fetch(
    `/api/research/artifacts?artifact=${encodeURIComponent(artifact)}`,
    { headers: { Authorization: `Bearer ${token}` } }
  );
  if (!res.ok) throw new Error(`Failed to fetch ${artifact}: ${res.status}`);
  return normalizeResearchArtifact<T>(await res.json() as JsonLike);
}

export const fetchResearchSummary = (t: string) =>
  fetchArtifact<ResearchSummary>('research-summary', t);

export const fetchPromotionManifest = (t: string) =>
  fetchArtifact<PromotionManifest>('promotion-manifest', t);

export const fetchReviewResolutionPlan = (t: string) =>
  fetchArtifact<ReviewResolutionPlan>('review-resolution-plan', t);

export const fetchReviewQueue = (t: string) =>
  fetchArtifact<ResearchReviewQueueItem[]>('review-queue', t);

export const fetchImportPreview = (t: string) =>
  fetchArtifact<PromotionImportPreview>('promotion-import-preview', t);

export const fetchDryRunReport = (t: string) =>
  fetchArtifact<PromotionImportDryRunReport>('import-dry-run/promotion-import-dry-run-report', t);

export const fetchExportManifest = (t: string) =>
  fetchArtifact<PromotionExportManifest>('promotion-export/promotion-export-manifest', t);

export const fetchPromotableSubstances = (t: string) =>
  fetchArtifact<unknown[]>('promotion-export/substances.promotable', t);

export const fetchEvidencePacket = (slug: string, t: string) =>
  fetchArtifact<EvidencePacket>(`evidence-packet/${slug}`, t);
