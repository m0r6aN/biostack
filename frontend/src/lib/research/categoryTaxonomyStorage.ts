import type {
  ResearchCategoryTaxonomy,
  ResearchCategoryTaxonomyAuditEntry,
  ResearchCategoryTaxonomyAuditLog,
} from './types';
import * as fs from 'node:fs/promises';
import path from 'node:path';

const AUDIT_SCHEMA_VERSION = '1.0.0';
const MAX_AUDIT_ENTRIES = 25;

function isMissingFileError(error: unknown) {
  return typeof error === 'object' && error !== null && 'code' in error && error.code === 'ENOENT';
}

export function repoRoot() {
  const cwd = process.cwd();
  return path.basename(cwd).toLowerCase() === 'frontend' ? path.resolve(cwd, '..') : cwd;
}

export function taxonomyPath(root: string) {
  return path.join(root, 'research', 'category-taxonomy.json');
}

export function taxonomyAuditPath(root: string) {
  return path.join(root, 'research', 'category-taxonomy-audit.json');
}

export async function readJsonFile<T>(filePath: string): Promise<T> {
  return JSON.parse(await fs.readFile(filePath, 'utf8')) as T;
}

export async function writeJsonFile(filePath: string, value: unknown) {
  await fs.mkdir(path.dirname(filePath), { recursive: true });
  const tempPath = `${filePath}.${process.pid}.tmp`;

  try {
    await fs.writeFile(tempPath, `${JSON.stringify(value, null, 2)}\n`, 'utf8');
    await fs.rename(tempPath, filePath);
  } catch (error) {
    await fs.unlink(tempPath).catch(() => undefined);
    throw error;
  }
}

export async function readCategoryTaxonomy(root: string) {
  try {
    return await readJsonFile<ResearchCategoryTaxonomy>(taxonomyPath(root));
  } catch (error) {
    if (isMissingFileError(error)) return null;
    throw error;
  }
}

export async function writeCategoryTaxonomy(root: string, taxonomy: ResearchCategoryTaxonomy) {
  await writeJsonFile(taxonomyPath(root), taxonomy);
}

export async function readCategoryTaxonomyAuditLog(root: string): Promise<ResearchCategoryTaxonomyAuditLog> {
  try {
    const parsed = await readJsonFile<ResearchCategoryTaxonomyAuditLog>(taxonomyAuditPath(root));
    return {
      schemaVersion: parsed.schemaVersion ?? AUDIT_SCHEMA_VERSION,
      updatedAtUtc: parsed.updatedAtUtc ?? new Date().toISOString(),
      entries: Array.isArray(parsed.entries) ? parsed.entries : [],
    };
  } catch (error) {
    if (isMissingFileError(error)) {
      return {
        schemaVersion: AUDIT_SCHEMA_VERSION,
        updatedAtUtc: new Date().toISOString(),
        entries: [],
      };
    }
    throw error;
  }
}

export async function appendCategoryTaxonomyAuditEntry(
  root: string,
  entry: Omit<ResearchCategoryTaxonomyAuditEntry, 'entryId' | 'createdAtUtc'>,
) {
  const createdAtUtc = new Date().toISOString();
  const log = await readCategoryTaxonomyAuditLog(root);
  const nextEntry: ResearchCategoryTaxonomyAuditEntry = {
    ...entry,
    entryId: `taxonomy-audit-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`,
    createdAtUtc,
  };

  const nextLog: ResearchCategoryTaxonomyAuditLog = {
    schemaVersion: AUDIT_SCHEMA_VERSION,
    updatedAtUtc: createdAtUtc,
    entries: [nextEntry, ...log.entries].slice(0, MAX_AUDIT_ENTRIES),
  };

  await writeJsonFile(taxonomyAuditPath(root), nextLog);
  return nextEntry;
}