import {
    appendCategoryTaxonomyAuditEntry,
    readCategoryTaxonomy,
    repoRoot,
    writeCategoryTaxonomy,
} from '@/lib/research/categoryTaxonomyStorage';
import type { ResearchCategoryDefinition, ResearchCategoryTaxonomy } from '@/lib/research/types';

export const runtime = 'nodejs';

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function normalizeKey(value: string) {
  return value.trim().toLowerCase().replace(/[\/_-]+/g, ' ').replace(/\s+/g, ' ');
}

function parseAliases(value: unknown) {
  if (Array.isArray(value)) {
    return value.flatMap((item) => typeof item === 'string' ? item.split(',') : []);
  }

  return typeof value === 'string' ? value.split(',') : [];
}

function parseOptionalString(value: unknown) {
  return typeof value === 'string' ? value.trim() : '';
}

function sanitizeCategories(value: unknown): ResearchCategoryDefinition[] {
  if (!Array.isArray(value)) return [];

  const seenCategories = new Set<string>();
  const sanitized: ResearchCategoryDefinition[] = [];

  for (const item of value) {
    if (!isRecord(item)) continue;

    const name = typeof item.name === 'string' ? item.name.trim() : '';
    if (!name) continue;

    const categoryKey = normalizeKey(name);
    if (seenCategories.has(categoryKey)) continue;
    seenCategories.add(categoryKey);
    const deprecated = item.deprecated === true;
    const replacedBy = parseOptionalString(item.replacedBy);
    const replacementKey = deprecated && replacedBy ? normalizeKey(replacedBy) : '';

    const seenAliases = new Set<string>();
    const aliases = parseAliases(item.aliases)
      .map((alias) => alias.trim())
      .filter((alias) => alias.length > 0)
      .filter((alias) => {
        const key = normalizeKey(alias);
        if (key === categoryKey || key === replacementKey || seenAliases.has(key)) return false;
        seenAliases.add(key);
        return true;
      });

    sanitized.push({
      name,
      aliases,
      ...(deprecated ? { deprecated: true } : {}),
      ...(deprecated && replacedBy && normalizeKey(replacedBy) !== categoryKey ? { replacedBy } : {}),
    });
  }

  return sanitized;
}

function sanitizeTaxonomy(body: unknown): ResearchCategoryTaxonomy | null {
  if (!isRecord(body)) return null;

  const categories = sanitizeCategories(body.categories);
  if (categories.length === 0) return null;

  return {
    taxonomyVersion: '1.0.0',
    updatedAtUtc: new Date().toISOString(),
    categories,
  };
}

export async function PUT(request: Request) {
  if (process.env.NODE_ENV === 'production') {
    return Response.json({ error: 'Not Found' }, { status: 404 });
  }

  let body: unknown;
  try {
    body = await request.json();
  } catch {
    return Response.json({ error: 'Invalid JSON body.' }, { status: 400 });
  }

  const taxonomy = sanitizeTaxonomy(body);
  if (!taxonomy) {
    return Response.json({ error: 'At least one valid category is required.' }, { status: 400 });
  }

  try {
    const root = repoRoot();
    const beforeTaxonomy = await readCategoryTaxonomy(root);
    await writeCategoryTaxonomy(root, taxonomy);
    await appendCategoryTaxonomyAuditEntry(root, {
      action: 'save-taxonomy',
      taxonomyVersion: taxonomy.taxonomyVersion,
      summary: `Saved taxonomy with ${taxonomy.categories.length} categories.`,
      beforeTaxonomy,
      afterTaxonomy: taxonomy,
    });
    return Response.json(taxonomy);
  } catch (error) {
    return Response.json({ error: error instanceof Error ? error.message : 'Failed to save category taxonomy.' }, { status: 500 });
  }
}