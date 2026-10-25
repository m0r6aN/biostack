import { redactDoseText } from '@/lib/doseRedaction';

/** Growth Phase 9 (03 §9): aggregated "What people report" themes. Never individual reports. */
export interface CommunityThemeView { id: string; label: string }
export interface CommunityThemes {
  compoundSlug: string;
  windowDays: number;
  minCount: number;
  submissions: number | null;
  sideEffects: CommunityThemeView[];
  benefits: CommunityThemeView[];
}

export const COMMUNITY_HEADER = 'What people report: anecdote, not evidence';
export const COMMUNITY_EMPTY = 'Not enough reports to show a pattern. We don\u2019t show single stories.';
export const COMMUNITY_UNVERIFIED = 'Unverified. Unknown product identity and purity.';

const DIGIT = /\d/;

function cleanList(value: unknown): CommunityThemeView[] {
  if (!Array.isArray(value)) return [];
  return value
    .filter((t): t is CommunityThemeView => !!t && typeof t.id === 'string' && typeof t.label === 'string')
    // Theme labels come from a controlled vocabulary; anything numeric is not ours, so drop it.
    .filter(t => !DIGIT.test(t.label))
    .map(t => ({ id: t.id, label: redactDoseText(t.label).trim() }))
    .filter(t => t.label.length > 0)
    .slice(0, 12);
}

/** Defensive parse: unknown shapes collapse to the empty state; N is shown only with at least one theme. */
export function normalizeCommunityThemes(raw: unknown, slug: string): CommunityThemes {
  const r = (raw && typeof raw === 'object' ? raw : {}) as Record<string, unknown>;
  const sideEffects = cleanList(r.sideEffects);
  const benefits = cleanList(r.benefits);
  const n = typeof r.submissions === 'number' && Number.isFinite(r.submissions) && r.submissions > 0 ? Math.floor(r.submissions) : null;
  const minCount = typeof r.minCount === 'number' && r.minCount >= 10 ? r.minCount : 10;
  return {
    compoundSlug: slug,
    windowDays: typeof r.windowDays === 'number' ? r.windowDays : 180,
    minCount,
    submissions: sideEffects.length + benefits.length > 0 && n !== null && n >= minCount ? n : null,
    sideEffects,
    benefits,
  };
}

export function hasCommunityThemes(t: CommunityThemes): boolean {
  return t.submissions !== null && t.sideEffects.length + t.benefits.length > 0;
}

export async function fetchCommunityThemes(slug: string, fetcher: typeof fetch = fetch): Promise<CommunityThemes> {
  try {
    const res = await fetcher(`/api/v1/community/${encodeURIComponent(slug)}/themes`, { headers: { Accept: 'application/json' } });
    if (!res.ok) return normalizeCommunityThemes(null, slug);
    return normalizeCommunityThemes(await res.json(), slug);
  } catch {
    return normalizeCommunityThemes(null, slug);
  }
}
