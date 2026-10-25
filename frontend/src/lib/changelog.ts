import { redactDoseText } from '@/lib/doseRedaction';
import type { KnowledgeEntry } from '@/lib/types';

export interface ChangelogRow {
  claimId: string; statement: string; version: number; verdict: string; reason: string;
  publishedAt: string; reviewers: string[];
}

/** Published versions only, newest first. Reasons never carry a dose (03 §6). */
export function changelogRows(entry: KnowledgeEntry): ChangelogRow[] {
  return (entry.details?.publicClaims ?? []).flatMap(claim =>
    (claim.changelog ?? [])
      .filter(c => Boolean(c.publishedAt))
      .map(c => ({
        claimId: claim.id, statement: redactDoseText(claim.statement), version: c.version, verdict: c.verdict,
        reason: redactDoseText(c.reason ?? ''), publishedAt: c.publishedAt as string, reviewers: c.reviewers ?? [],
      })))
    .sort((a, b) => b.publishedAt.localeCompare(a.publishedAt) || b.version - a.version);
}
