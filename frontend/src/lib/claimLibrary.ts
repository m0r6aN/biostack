import { getApiBaseUrl } from '@/lib/apiBase';
import { isPublished } from '@/lib/claimVerdicts';
import { compoundSlug } from '@/lib/compoundSlug';
import type { KnowledgeEntry, KnowledgePublicClaim } from '@/lib/types';

/** One public claim with the compound it belongs to. */
export interface LibraryClaim {
  entry: KnowledgeEntry;
  slug: string;
  claim: KnowledgePublicClaim;
}

/** Server-side: every public compound (best effort; [] when the API is unreachable). */
export async function fetchAllEntries(): Promise<KnowledgeEntry[]> {
  try {
    const response = await fetch(`${getApiBaseUrl()}/api/v1/knowledge/compounds`, {
      next: { revalidate: 3600 },
      signal: AbortSignal.timeout(8000),
    });
    if (!response.ok) return [];
    const data = (await response.json()) as KnowledgeEntry[];
    return Array.isArray(data) ? data : [];
  } catch {
    return [];
  }
}

export function claimLibrary(entries: KnowledgeEntry[]): LibraryClaim[] {
  return entries.flatMap(entry =>
    (entry.details?.publicClaims ?? []).map(claim => ({ entry, slug: compoundSlug(entry.canonicalName), claim })));
}

export function findClaim(entries: KnowledgeEntry[], claimId: string): LibraryClaim | null {
  return claimLibrary(entries).find(c => c.claim.id === claimId) ?? null;
}

/** "Re-check due" claims are stale and excluded from rotations and lists (06 §D). */
export function isFresh(claim: KnowledgePublicClaim, now: Date = new Date()): boolean {
  if (!claim.recheckDueAt) return true;
  const due = new Date(claim.recheckDueAt);
  return Number.isNaN(due.getTime()) || due.getTime() > now.getTime();
}

/** Published, fresh claims only: the sole input to public lists, rotations and cards. */
export function publishedLibrary(entries: KnowledgeEntry[], now: Date = new Date()): LibraryClaim[] {
  return claimLibrary(entries).filter(c => isPublished(c.claim) && isFresh(c.claim, now));
}
