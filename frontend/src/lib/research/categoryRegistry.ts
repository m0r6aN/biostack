import type { ResearchCategoryTaxonomy } from './types';

function normalizeKey(value: string) {
  return value.trim().toLowerCase().replace(/[\/_-]+/g, ' ').replace(/\s+/g, ' ');
}

function aliasMap(taxonomy: ResearchCategoryTaxonomy | null | undefined) {
  const entries = new Map<string, string>();

  for (const category of taxonomy?.categories ?? []) {
    if (!category.name.trim()) continue;
    const canonicalTarget = category.deprecated && category.replacedBy?.trim()
      ? category.replacedBy.trim()
      : category.name;
    entries.set(normalizeKey(category.name), canonicalTarget);
    for (const alias of category.aliases ?? []) {
      if (!alias.trim()) continue;
      entries.set(normalizeKey(alias), canonicalTarget);
    }
  }

  return entries;
}

export function getResearchCategoryPresets(taxonomy: ResearchCategoryTaxonomy | null | undefined) {
  return (taxonomy?.categories ?? [])
    .filter((category) => !category.deprecated)
    .map((category) => category.name);
}

export function normalizeResearchCategory(taxonomy: ResearchCategoryTaxonomy | null | undefined, value: string) {
  const trimmed = value.trim();
  if (!trimmed) return '';
  return aliasMap(taxonomy).get(normalizeKey(trimmed)) ?? trimmed;
}

export function normalizeResearchCategories(taxonomy: ResearchCategoryTaxonomy | null | undefined, value: string | readonly string[]) {
  const items = Array.isArray(value)
    ? value
    : value.split(',').map((item) => item.trim());

  const seen = new Set<string>();
  const normalized: string[] = [];

  for (const item of items) {
    const category = normalizeResearchCategory(taxonomy, item);
    if (!category) continue;
    const key = category.toLowerCase();
    if (seen.has(key)) continue;
    seen.add(key);
    normalized.push(category);
  }

  return normalized;
}

export function appendResearchCategory(taxonomy: ResearchCategoryTaxonomy | null | undefined, current: string, category: string) {
  return normalizeResearchCategories(taxonomy, [...normalizeResearchCategories(taxonomy, current), category]).join(', ');
}

export function mergeResearchCategoryOptions(taxonomy: ResearchCategoryTaxonomy | null | undefined, ...groups: ReadonlyArray<readonly string[]>) {
  return normalizeResearchCategories(taxonomy, [
    ...getResearchCategoryPresets(taxonomy),
    ...groups.flatMap((group) => group),
  ]).sort((a, b) => a.localeCompare(b));
}