import type { LibraryClaim } from '@/lib/claimLibrary';
import type { KnowledgeEntry } from '@/lib/types';

// Growth Phase 7 (03 §7) v1: match a pasted claim against the curated, published library only.
// Nothing here creates a verdict; every result is an existing reviewed claim.

export const NO_MATCH_COPY = "We haven't ruled on that one. Send it in and we'll check the receipts.";
export const PERSONAL_ADVICE_COPY = "BioStack grades evidence. It doesn't tell you what to take. That's between you and a clinician.";
export const MAX_RESULTS = 5;
export const MIN_SCORE = 0.2;

const DOSE_INTENT = /\b\d+(?:[.,]\d+)?\s?(?:mg|mcg|µg|ug|iu|units?|ml)\b|\bhow much\b|\bdos(?:e|es|ing|age)\b|\bcycles?\b|\bstack(?:ed|ing)? with\b/i;
const PERSONAL_ADVICE = /\bshould i\b|\bsafe for me\b|\bcan i (?:take|use|run)\b|\bright for me\b|\bwill it work for me\b/i;

const STOP = new Set('a an the and or of for to in on with without is are was were be been it its this that does do did has have had no not can will just my your their at by from as than then so very really'.split(' '));
const SYNONYMS: Record<string, string> = {
  fat: 'weight', weightloss: 'weight', slim: 'weight', heal: 'healing', heals: 'healing', repair: 'healing', recovery: 'healing',
  tendons: 'tendon', ligament: 'tendon', muscles: 'muscle', gains: 'muscle', sleep: 'sleep', insomnia: 'sleep',
  sideeffects: 'side', effects: 'effect', safe: 'safety', danger: 'safety', dangerous: 'safety', cancer: 'cancer', tumor: 'cancer',
  aging: 'longevity', antiaging: 'longevity', lifespan: 'longevity', libido: 'sexual', skin: 'skin', hair: 'hair',
};

export function normalize(text: string): string {
  return text.toLowerCase().replace(/[\u2018\u2019]/g, "'").replace(/[^a-z0-9+\-\s']/g, ' ').replace(/\s+/g, ' ').trim();
}
const squash = (s: string) => s.toLowerCase().replace(/[^a-z0-9]/g, '');

function stem(word: string): string {
  const w = SYNONYMS[word] ?? word;
  return SYNONYMS[w] ?? (w.length > 4 ? w.replace(/(?:ing|ed|es|s)$/, '') : w);
}

export function tokens(text: string): string[] {
  return normalize(text).split(' ').filter(t => t && !STOP.has(t)).map(stem);
}

export function hasDoseIntent(q: string): boolean { return DOSE_INTENT.test(q); }
export function asksPersonalAdvice(q: string): boolean { return PERSONAL_ADVICE.test(q); }

/** Compounds named in the query, by canonical name or alias ("MK-677" → Ibutamoren), hyphen/space-insensitive. */
export function detectCompounds(q: string, entries: KnowledgeEntry[]): KnowledgeEntry[] {
  const nq = ` ${normalize(q)} `;
  const sq = squash(q);
  return entries.filter(e => [e.canonicalName, ...(e.aliases ?? [])].some(name => {
    const n = normalize(name ?? '');
    if (n.length < 3) return false;
    return nq.includes(` ${n} `) || (squash(n).length >= 5 && sq.includes(squash(n)));
  }));
}

export interface CheckMatch { item: LibraryClaim; score: number }

export function matchClaims(q: string, library: LibraryClaim[], entries: KnowledgeEntry[]): CheckMatch[] {
  const named = new Set(detectCompounds(q, entries).map(e => e.canonicalName));
  const compoundWords = new Set([...named].flatMap(n => {
    const e = entries.find(x => x.canonicalName === n);
    return [n, ...(e?.aliases ?? [])].flatMap(tokens);
  }));
  const qt = new Set(tokens(q).filter(t => !compoundWords.has(t)));
  return library
    .map(item => {
      const ct = new Set(tokens(`${item.claim.statement} ${item.claim.outcome ?? ''} ${item.claim.population ?? ''}`).filter(t => !compoundWords.has(t)));
      const shared = [...qt].filter(t => ct.has(t)).length;
      const overlap = qt.size === 0 ? 0 : shared / Math.max(1, Math.min(qt.size, ct.size));
      const compoundHit = named.has(item.entry.canonicalName);
      if (named.size > 0 && !compoundHit) return { item, score: 0 };
      const score = (compoundHit ? 0.35 : 0) + 0.65 * overlap;
      return { item, score };
    })
    .filter(m => m.score >= MIN_SCORE)
    .sort((a, b) => b.score - a.score || a.item.claim.id.localeCompare(b.item.claim.id))
    .slice(0, MAX_RESULTS);
}

export interface CheckResult {
  query: string;
  doseIntent: boolean;
  personalAdvice: boolean;
  matches: CheckMatch[];
  /** Compound named in the query (for pre-filling the submit form and the trial-conditions link). */
  compound: KnowledgeEntry | null;
}

export function checkClaim(q: string, library: LibraryClaim[], entries: KnowledgeEntry[]): CheckResult {
  const query = q.trim().slice(0, 500);
  const compound = detectCompounds(query, entries)[0] ?? null;
  return {
    query,
    doseIntent: hasDoseIntent(query),
    personalAdvice: asksPersonalAdvice(query),
    matches: query.length >= 2 ? matchClaims(query, library, entries) : [],
    compound,
  };
}
