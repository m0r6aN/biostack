import type {
  ResearchSummary,
  PromotionManifest,
  ReviewResolutionPlan,
  PromotionImportPreview,
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
