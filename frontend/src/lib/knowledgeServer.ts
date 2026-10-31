import { getApiBaseUrl } from '@/lib/apiBase';
import type { KnowledgeEntry } from '@/lib/types';

export function safeDecodeSlug(value: string): string {
  try {
    return decodeURIComponent(value);
  } catch {
    return value;
  }
}

/** Server-side dossier fetch (best effort; null when the API is unreachable or the compound is unknown). */
export type KnowledgeEntryLookup = {
  entry: KnowledgeEntry | null;
  /** True only when the API answered 404: the record does not exist or is not public. A timeout or 5xx is not "missing". */
  missing: boolean;
};

export async function lookupKnowledgeEntry(slug: string): Promise<KnowledgeEntryLookup> {
  try {
    const response = await fetch(
      `${getApiBaseUrl()}/api/v1/knowledge/compounds/${encodeURIComponent(safeDecodeSlug(slug))}`,
      {
        next: { revalidate: 3600 },
        signal: AbortSignal.timeout(5000),
      }
    );

    if (!response.ok) {
      return { entry: null, missing: response.status === 404 };
    }

    return { entry: (await response.json()) as KnowledgeEntry, missing: false };
  } catch {
    return { entry: null, missing: false };
  }
}

export async function fetchKnowledgeEntry(slug: string): Promise<KnowledgeEntry | null> {
  return (await lookupKnowledgeEntry(slug)).entry;
}

/** Version date of a dossier: when the record was last reviewed (falls back to null). */
export function dossierVersionDate(entry: KnowledgeEntry): string | null {
  const reviewed = entry.details?.lastReviewedAt;
  if (!reviewed) return null;
  const parsed = new Date(reviewed);
  return Number.isNaN(parsed.getTime()) ? null : parsed.toISOString().slice(0, 10);
}
