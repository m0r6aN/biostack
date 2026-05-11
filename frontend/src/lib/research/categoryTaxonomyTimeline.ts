import { diffCategoryTaxonomyAuditEntry } from './categoryTaxonomyDiff';
import type { ResearchCategoryTaxonomyAuditEntry, ResearchCategoryTaxonomyAuditLog } from './types';

export function matchesTaxonomyAuditSearch(entry: ResearchCategoryTaxonomyAuditEntry, query: string) {
  const normalizedQuery = query.trim().toLowerCase();
  if (!normalizedQuery) return true;

  const diff = diffCategoryTaxonomyAuditEntry(entry);
  const searchable = [
    entry.summary,
    entry.action,
    entry.taxonomyVersion,
    ...diff.added.flatMap((name) => [name, `added ${name}`]),
    ...diff.removed.flatMap((name) => [name, `removed ${name}`]),
    ...diff.deprecated.flatMap((name) => [name, `deprecated ${name}`]),
    ...diff.restored.flatMap((name) => [name, `restored ${name}`]),
    ...diff.aliasChanged.flatMap((name) => [name, `aliases changed ${name}`]),
    ...diff.renamed.flatMap((change) => [change.before, change.after, `renamed ${change.before} ${change.after}`]),
    ...diff.replacementChanged.flatMap((change) => [change.name, change.before, change.after, `replacement ${change.name} ${change.before} ${change.after}`]),
  ]
    .join(' ')
    .toLowerCase();

  return searchable.includes(normalizedQuery);
}

export function buildJsonDownloadHref(payload: unknown) {
  return `data:application/json;charset=utf-8,${encodeURIComponent(JSON.stringify(payload, null, 2))}`;
}

export function buildTaxonomyAuditLogExport(log: ResearchCategoryTaxonomyAuditLog | null) {
  return {
    exportType: 'taxonomy-audit-log',
    exportedAtUtc: new Date().toISOString(),
    auditLog: log,
  };
}

export function buildFilteredTaxonomyAuditExport(entries: ResearchCategoryTaxonomyAuditEntry[], activeFilter: string, searchQuery: string) {
  return {
    exportType: 'taxonomy-audit-filtered-stream',
    exportedAtUtc: new Date().toISOString(),
    filters: {
      action: activeFilter,
      searchQuery: searchQuery.trim() || null,
    },
    entries: entries.map((entry) => ({
      ...entry,
      diff: diffCategoryTaxonomyAuditEntry(entry),
    })),
  };
}

export function buildFocusedTaxonomyAuditExport(entry: ResearchCategoryTaxonomyAuditEntry | null) {
  if (!entry) return null;

  return {
    exportType: 'taxonomy-audit-entry',
    exportedAtUtc: new Date().toISOString(),
    entry,
    diff: diffCategoryTaxonomyAuditEntry(entry),
  };
}