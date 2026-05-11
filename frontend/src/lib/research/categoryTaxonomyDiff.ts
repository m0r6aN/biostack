import type {
    ResearchCategoryTaxonomy,
    ResearchCategoryTaxonomyAuditEntry,
} from './types';

export interface ResearchCategoryTaxonomyAuditDiff {
  added: string[];
  removed: string[];
  renamed: Array<{ before: string; after: string }>;
  deprecated: string[];
  restored: string[];
  replacementChanged: Array<{ name: string; before: string; after: string }>;
  aliasChanged: string[];
  migrationDelta: null | {
    totalFindings: number;
    requestFindings: number;
    taskItemFindings: number;
    resolvedTaskItemFindings: number;
  };
}

type CategorySnapshot = {
  name: string;
  aliases: Set<string>;
  deprecated: boolean;
  replacedBy: string;
};

function normalizeKey(value: string) {
  return value.trim().toLowerCase().replace(/[\/_-]+/g, ' ').replace(/\s+/g, ' ');
}

function toCategoryMap(taxonomy: ResearchCategoryTaxonomy | null | undefined) {
  const categories = new Map<string, CategorySnapshot>();

  for (const category of taxonomy?.categories ?? []) {
    const key = normalizeKey(category.name);
    if (!key) continue;
    categories.set(key, {
      name: category.name,
      aliases: new Set((category.aliases ?? []).map((alias) => normalizeKey(alias)).filter(Boolean)),
      deprecated: category.deprecated === true,
      replacedBy: category.replacedBy?.trim() ?? '',
    });
  }

  return categories;
}

function aliasSetsEqual(left: Set<string>, right: Set<string>) {
  if (left.size !== right.size) return false;
  for (const value of left) {
    if (!right.has(value)) return false;
  }
  return true;
}

function renameScore(before: CategorySnapshot, after: CategorySnapshot) {
  let score = 0;
  const beforeKey = normalizeKey(before.name);
  const afterKey = normalizeKey(after.name);

  if (after.aliases.has(beforeKey) || before.aliases.has(afterKey)) score += 3;
  if (before.replacedBy && normalizeKey(before.replacedBy) === afterKey) score += 2;
  if (after.replacedBy && normalizeKey(after.replacedBy) === beforeKey) score += 2;

  for (const alias of before.aliases) {
    if (after.aliases.has(alias)) score += 1;
  }

  return score;
}

function migrationDelta(entry: ResearchCategoryTaxonomyAuditEntry) {
  if (!entry.beforeMigrationReport || !entry.afterMigrationReport) return null;

  return {
    totalFindings: entry.afterMigrationReport.counts.totalFindings - entry.beforeMigrationReport.counts.totalFindings,
    requestFindings: entry.afterMigrationReport.counts.requestFindings - entry.beforeMigrationReport.counts.requestFindings,
    taskItemFindings: entry.afterMigrationReport.counts.taskItemFindings - entry.beforeMigrationReport.counts.taskItemFindings,
    resolvedTaskItemFindings: entry.afterMigrationReport.counts.resolvedTaskItemFindings - entry.beforeMigrationReport.counts.resolvedTaskItemFindings,
  };
}

export function diffCategoryTaxonomyAuditEntry(entry: ResearchCategoryTaxonomyAuditEntry): ResearchCategoryTaxonomyAuditDiff {
  const before = toCategoryMap(entry.beforeTaxonomy);
  const after = toCategoryMap(entry.afterTaxonomy);

  const addedKeys = [...after.keys()].filter((key) => !before.has(key));
  const removedKeys = [...before.keys()].filter((key) => !after.has(key));
  const usedAdded = new Set<string>();
  const usedRemoved = new Set<string>();
  const renamed: Array<{ before: string; after: string }> = [];

  for (const removedKey of removedKeys) {
    const beforeCategory = before.get(removedKey);
    if (!beforeCategory) continue;

    let bestMatch: string | null = null;
    let bestScore = 0;

    for (const addedKey of addedKeys) {
      if (usedAdded.has(addedKey)) continue;
      const afterCategory = after.get(addedKey);
      if (!afterCategory) continue;
      const score = renameScore(beforeCategory, afterCategory);
      if (score > bestScore) {
        bestScore = score;
        bestMatch = addedKey;
      }
    }

    if (!bestMatch || bestScore <= 0) continue;
    usedRemoved.add(removedKey);
    usedAdded.add(bestMatch);
    renamed.push({ before: beforeCategory.name, after: after.get(bestMatch)?.name ?? bestMatch });
  }

  const deprecated: string[] = [];
  const restored: string[] = [];
  const replacementChanged: Array<{ name: string; before: string; after: string }> = [];
  const aliasChanged: string[] = [];

  for (const [key, beforeCategory] of before) {
    const afterCategory = after.get(key);
    if (!afterCategory) continue;

    if (!beforeCategory.deprecated && afterCategory.deprecated) deprecated.push(afterCategory.name);
    if (beforeCategory.deprecated && !afterCategory.deprecated) restored.push(afterCategory.name);
    if (beforeCategory.replacedBy !== afterCategory.replacedBy) {
      replacementChanged.push({ name: afterCategory.name, before: beforeCategory.replacedBy || '—', after: afterCategory.replacedBy || '—' });
    }
    if (!aliasSetsEqual(beforeCategory.aliases, afterCategory.aliases)) aliasChanged.push(afterCategory.name);
  }

  return {
    added: addedKeys.filter((key) => !usedAdded.has(key)).map((key) => after.get(key)?.name ?? key).sort((a, b) => a.localeCompare(b)),
    removed: removedKeys.filter((key) => !usedRemoved.has(key)).map((key) => before.get(key)?.name ?? key).sort((a, b) => a.localeCompare(b)),
    renamed: renamed.sort((a, b) => a.before.localeCompare(b.before)),
    deprecated: deprecated.sort((a, b) => a.localeCompare(b)),
    restored: restored.sort((a, b) => a.localeCompare(b)),
    replacementChanged: replacementChanged.sort((a, b) => a.name.localeCompare(b.name)),
    aliasChanged: aliasChanged.sort((a, b) => a.localeCompare(b)),
    migrationDelta: migrationDelta(entry),
  };
}

export function hasCategoryTaxonomyAuditDiff(diff: ResearchCategoryTaxonomyAuditDiff) {
  return diff.added.length > 0
    || diff.removed.length > 0
    || diff.renamed.length > 0
    || diff.deprecated.length > 0
    || diff.restored.length > 0
    || diff.replacementChanged.length > 0
    || diff.aliasChanged.length > 0
    || diff.migrationDelta !== null;
}