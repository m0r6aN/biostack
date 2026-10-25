import { compoundSlug } from '@/lib/compoundSlug';
import { tierLabel } from '@/lib/evidenceTiers';
import { dossierVersionDate } from '@/lib/knowledgeServer';
import type { KnowledgeEntry } from '@/lib/types';

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
  return truncate(`${grade} ${entry.mechanismSummary}`.trim(), 155);
}

export function dossierCardUrl(entry: KnowledgeEntry): string {
  const version = dossierVersionDate(entry);
  const slug = compoundSlug(entry.canonicalName);
  return version ? `/og/knowledge/${slug}?v=${version}` : `/og/knowledge/${slug}`;
}

