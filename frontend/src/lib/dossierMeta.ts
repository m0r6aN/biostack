import { compoundSlug } from '@/lib/compoundSlug';
import { tierLabel } from '@/lib/evidenceTiers';
import { dossierVersionDate } from '@/lib/knowledgeServer';
import type { KnowledgeEntry } from '@/lib/types';
import { headlineClaim, isPublished } from '@/lib/claimVerdicts';
import { buildCardModel, cardImageUrl } from '@/lib/verdictCard';
import { redactDoseText } from '@/lib/doseRedaction';

function truncate(value: string, max: number): string {
  if (value.length <= max) {
    return value;
  }

  return `${value.slice(0, max - 1).trimEnd()}…`;
}

export function dossierTitle(name: string): string {
  return `${name}: what the evidence actually says | BioStack`;
}

export function dossierDescription(entry: KnowledgeEntry): string {
  const tier = tierLabel(entry.evidenceTier);
  const grade = tier === 'Not graded' ? 'Not yet graded by BioStack.' : `BioStack evidence grade: ${tier}.`;
  return truncate(redactDoseText(`${grade} ${entry.mechanismSummary}`).trim(), 155);
}

/** og:image = the Phase 3 compound verdict card; cache key includes the headline claim version. */
export function dossierCardUrl(entry: KnowledgeEntry): string {
  const slug = compoundSlug(entry.canonicalName);
  const headline = headlineClaim(entry.details?.publicClaims);
  if (headline) return cardImageUrl('compound', slug, headline.version);
  const version = dossierVersionDate(entry);
  return version ? `/og/compound/${slug}?v=${version}` : `/og/compound/${slug}`;
}

/** Card alt text = the full verdict sentence (03 §3); falls back to the grade line. */
export function dossierCardAlt(entry: KnowledgeEntry): string {
  try {
    return buildCardModel({ kind: 'compound', entry }).alt;
  } catch {
    return `BioStack verdict card for ${entry.canonicalName}`;
  }
}

/** 03 §2 title: "[Compound] for [outcome]: [VERDICT] | BioStack" once a headline is published. */
export function dossierShareTitle(entry: KnowledgeEntry): string {
  const headline = headlineClaim(entry.details?.publicClaims);
  if (headline && isPublished(headline) && headline.outcome) {
    return `${entry.canonicalName} for ${headline.outcome}: ${headline.verdict} | BioStack`;
  }
  return dossierTitle(entry.canonicalName);
}

