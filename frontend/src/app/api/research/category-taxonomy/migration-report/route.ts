import {
    appendCategoryTaxonomyAuditEntry,
    readCategoryTaxonomy,
    readJsonFile,
    repoRoot,
    writeJsonFile,
} from '@/lib/research/categoryTaxonomyStorage';
import type {
    ResearchCategoryMigrationApplyFileReceipt,
    ResearchCategoryMigrationApplyReceipt,
    ResearchCategoryMigrationCategorySummary,
    ResearchCategoryMigrationFinding,
    ResearchCategoryMigrationReport,
    ResearchCategoryTaxonomy,
} from '@/lib/research/types';
import path from 'node:path';

export const runtime = 'nodejs';

type DeprecatedCategoryMapping = {
  deprecatedCategory: string;
  replacementCategory: string;
};

type RequestArtifact = {
  file: string;
  payload: {
    requests?: Array<{ requestId?: string; compoundName?: string; categories?: string[] }>;
  };
};

type TaskArtifact = {
  file: string;
  payload: {
    items?: Array<{ taskId?: string; compoundName?: string; categories?: string[] }>;
    resolvedItems?: Array<{ taskId?: string; compoundName?: string; categories?: string[] }>;
  };
};

function normalizeKey(value: string) {
  return value.trim().toLowerCase().replace(/[\/_-]+/g, ' ').replace(/\s+/g, ' ');
}

function relativePath(root: string, target: string) {
  return path.relative(root, target).split(path.sep).join('/');
}

async function readJsonFiles(directory: string): Promise<string[]> {
  try {
    const { readdir } = await import('node:fs/promises');
    const entries = await readdir(directory, { withFileTypes: true });
    return entries
      .filter((entry) => entry.isFile() && entry.name.toLowerCase().endsWith('.json'))
      .map((entry) => path.join(directory, entry.name));
  } catch (error) {
    if (typeof error === 'object' && error !== null && 'code' in error && error.code === 'ENOENT') {
      return [];
    }
    throw error;
  }
}

function buildDeprecatedMappings(taxonomy: ResearchCategoryTaxonomy) {
  const mappings = new Map<string, DeprecatedCategoryMapping>();

  for (const category of taxonomy.categories) {
    const replacementCategory = category.deprecated && category.replacedBy?.trim()
      ? category.replacedBy.trim()
      : '';
    if (!replacementCategory) continue;

    const mapping = { deprecatedCategory: category.name, replacementCategory };
    mappings.set(normalizeKey(category.name), mapping);

    for (const alias of category.aliases ?? []) {
      if (!alias.trim()) continue;
      mappings.set(normalizeKey(alias), mapping);
    }
  }

  return mappings;
}

function categoryFinding(
  mapping: DeprecatedCategoryMapping | undefined,
  matchedCategory: string,
  details: Omit<ResearchCategoryMigrationFinding, 'matchedCategory' | 'deprecatedCategory' | 'replacementCategory'>,
) {
  if (!mapping) return null;

  return {
    ...details,
    matchedCategory,
    deprecatedCategory: mapping.deprecatedCategory,
    replacementCategory: mapping.replacementCategory,
  } satisfies ResearchCategoryMigrationFinding;
}

async function loadRequestArtifacts(root: string) {
  const files = await readJsonFiles(path.join(root, 'research', 'research-requests'));
  const artifacts: RequestArtifact[] = [];

  for (const file of files) {
    artifacts.push({
      file,
      payload: await readJsonFile<RequestArtifact['payload']>(file),
    });
  }

  return artifacts;
}

async function loadTaskArtifacts(root: string) {
  const candidateRoots = [...new Set([
    path.resolve(root, process.env.RESEARCH_ARTIFACTS_PATH ?? 'research/pilot'),
    path.resolve(root, 'research/output/latest'),
  ])];
  const artifacts: TaskArtifact[] = [];

  for (const candidateRoot of candidateRoots) {
    const file = path.join(candidateRoot, 'research-task-queue.json');
    try {
      artifacts.push({
        file,
        payload: await readJsonFile<TaskArtifact['payload']>(file),
      });
    } catch (error) {
      if (typeof error === 'object' && error !== null && 'code' in error && error.code === 'ENOENT') continue;
      throw error;
    }
  }

  return artifacts;
}

function scanRequestFiles(root: string, artifacts: RequestArtifact[], mappings: Map<string, DeprecatedCategoryMapping>) {
  const findings: ResearchCategoryMigrationFinding[] = [];

  for (const artifact of artifacts) {
    for (const request of artifact.payload.requests ?? []) {
      for (const category of request.categories ?? []) {
        const finding = categoryFinding(mappings.get(normalizeKey(category)), category, {
          sourceType: 'request',
          sourcePath: relativePath(root, artifact.file),
          compoundName: request.compoundName ?? 'Unknown Compound',
          requestId: request.requestId,
        });
        if (finding) findings.push(finding);
      }
    }
  }

  return { filesScanned: artifacts.length, findings };
}

function scanTaskArtifacts(root: string, artifacts: TaskArtifact[], mappings: Map<string, DeprecatedCategoryMapping>) {
  const findings: ResearchCategoryMigrationFinding[] = [];

  for (const artifact of artifacts) {
    for (const item of artifact.payload.items ?? []) {
      for (const category of item.categories ?? []) {
        const finding = categoryFinding(mappings.get(normalizeKey(category)), category, {
          sourceType: 'task-item',
          sourcePath: relativePath(root, artifact.file),
          compoundName: item.compoundName ?? 'Unknown Compound',
          taskId: item.taskId,
        });
        if (finding) findings.push(finding);
      }
    }

    for (const item of artifact.payload.resolvedItems ?? []) {
      for (const category of item.categories ?? []) {
        const finding = categoryFinding(mappings.get(normalizeKey(category)), category, {
          sourceType: 'resolved-task-item',
          sourcePath: relativePath(root, artifact.file),
          compoundName: item.compoundName ?? 'Unknown Compound',
          taskId: item.taskId,
        });
        if (finding) findings.push(finding);
      }
    }
  }

  return { filesScanned: artifacts.length, findings };
}

function summarizeFindings(findings: ResearchCategoryMigrationFinding[]) {
  const summary = new Map<string, ResearchCategoryMigrationCategorySummary>();

  for (const finding of findings) {
    const key = `${finding.deprecatedCategory}=>${finding.replacementCategory}`;
    const current = summary.get(key) ?? {
      deprecatedCategory: finding.deprecatedCategory,
      replacementCategory: finding.replacementCategory,
      findings: 0,
      requestFindings: 0,
      taskItemFindings: 0,
      resolvedTaskItemFindings: 0,
    };

    current.findings += 1;
    if (finding.sourceType === 'request') current.requestFindings += 1;
    else if (finding.sourceType === 'task-item') current.taskItemFindings += 1;
    else current.resolvedTaskItemFindings += 1;

    summary.set(key, current);
  }

  return [...summary.values()].sort((a, b) => b.findings - a.findings || a.deprecatedCategory.localeCompare(b.deprecatedCategory));
}

function buildReport(
  root: string,
  taxonomy: ResearchCategoryTaxonomy,
  requestArtifacts: RequestArtifact[],
  taskArtifacts: TaskArtifact[],
  mappings: Map<string, DeprecatedCategoryMapping>,
): ResearchCategoryMigrationReport {
  const requestScan = scanRequestFiles(root, requestArtifacts, mappings);
  const taskScan = scanTaskArtifacts(root, taskArtifacts, mappings);
  const findings = [...requestScan.findings, ...taskScan.findings];

  return {
    generatedAtUtc: new Date().toISOString(),
    taxonomyVersion: taxonomy.taxonomyVersion,
    counts: {
      requestFilesScanned: requestScan.filesScanned,
      requestFindings: requestScan.findings.length,
      taskArtifactsScanned: taskScan.filesScanned,
      taskItemFindings: taskScan.findings.filter((finding) => finding.sourceType === 'task-item').length,
      resolvedTaskItemFindings: taskScan.findings.filter((finding) => finding.sourceType === 'resolved-task-item').length,
      totalFindings: findings.length,
    },
    deprecatedCategories: summarizeFindings(findings),
    findings,
  };
}

function rewriteCategories(
  sourcePath: string,
  categories: string[] | undefined,
  mappings: Map<string, DeprecatedCategoryMapping>,
  details: Omit<ResearchCategoryMigrationFinding, 'sourcePath' | 'matchedCategory' | 'deprecatedCategory' | 'replacementCategory'>,
) {
  if (!Array.isArray(categories) || categories.length === 0) {
    return { categories: categories ?? [], rewrites: [] as ResearchCategoryMigrationFinding[] };
  }

  const rewrites: ResearchCategoryMigrationFinding[] = [];
  const deduped: string[] = [];
  const seen = new Set<string>();

  for (const category of categories) {
    const mapping = mappings.get(normalizeKey(category));
    const nextCategory = mapping?.replacementCategory ?? category;
    const nextKey = normalizeKey(nextCategory);

    if (mapping) {
      rewrites.push({
        ...details,
        sourcePath,
        matchedCategory: category,
        deprecatedCategory: mapping.deprecatedCategory,
        replacementCategory: mapping.replacementCategory,
      });
    }

    if (!nextKey || seen.has(nextKey)) continue;
    seen.add(nextKey);
    deduped.push(nextCategory);
  }

  return { categories: deduped, rewrites };
}

async function applyRequestMigrations(root: string, artifacts: RequestArtifact[], mappings: Map<string, DeprecatedCategoryMapping>) {
  const updatedFiles: ResearchCategoryMigrationApplyFileReceipt[] = [];

  for (const artifact of artifacts) {
    const rewrites: ResearchCategoryMigrationFinding[] = [];
    const compounds = new Set<string>();

    for (const request of artifact.payload.requests ?? []) {
      const result = rewriteCategories(relativePath(root, artifact.file), request.categories, mappings, {
        sourceType: 'request',
        compoundName: request.compoundName ?? 'Unknown Compound',
        requestId: request.requestId,
      });
      request.categories = result.categories;
      rewrites.push(...result.rewrites);
      if (result.rewrites.length > 0) compounds.add(request.compoundName ?? 'Unknown Compound');
    }

    if (rewrites.length === 0) continue;

    await writeJsonFile(artifact.file, artifact.payload);
    updatedFiles.push({
      sourceType: 'request-file',
      sourcePath: relativePath(root, artifact.file),
      categoriesRewritten: rewrites.length,
      compounds: [...compounds].sort((a, b) => a.localeCompare(b)),
      rewrites,
    });
  }

  return updatedFiles;
}

async function applyTaskMigrations(root: string, artifacts: TaskArtifact[], mappings: Map<string, DeprecatedCategoryMapping>) {
  const updatedFiles: ResearchCategoryMigrationApplyFileReceipt[] = [];

  for (const artifact of artifacts) {
    const rewrites: ResearchCategoryMigrationFinding[] = [];
    const compounds = new Set<string>();

    for (const item of artifact.payload.items ?? []) {
      const result = rewriteCategories(relativePath(root, artifact.file), item.categories, mappings, {
        sourceType: 'task-item',
        compoundName: item.compoundName ?? 'Unknown Compound',
        taskId: item.taskId,
      });
      item.categories = result.categories;
      rewrites.push(...result.rewrites);
      if (result.rewrites.length > 0) compounds.add(item.compoundName ?? 'Unknown Compound');
    }

    for (const item of artifact.payload.resolvedItems ?? []) {
      const result = rewriteCategories(relativePath(root, artifact.file), item.categories, mappings, {
        sourceType: 'resolved-task-item',
        compoundName: item.compoundName ?? 'Unknown Compound',
        taskId: item.taskId,
      });
      item.categories = result.categories;
      rewrites.push(...result.rewrites);
      if (result.rewrites.length > 0) compounds.add(item.compoundName ?? 'Unknown Compound');
    }

    if (rewrites.length === 0) continue;

    await writeJsonFile(artifact.file, artifact.payload);
    updatedFiles.push({
      sourceType: 'task-artifact',
      sourcePath: relativePath(root, artifact.file),
      categoriesRewritten: rewrites.length,
      compounds: [...compounds].sort((a, b) => a.localeCompare(b)),
      rewrites,
    });
  }

  return updatedFiles;
}

export async function GET() {
  if (process.env.NODE_ENV === 'production') {
    return Response.json({ error: 'Not Found' }, { status: 404 });
  }

  try {
    const root = repoRoot();
    const taxonomy = await readCategoryTaxonomy(root);
    if (!taxonomy) {
      return Response.json({ error: 'Category taxonomy not found.' }, { status: 404 });
    }
    const mappings = buildDeprecatedMappings(taxonomy);
    const requestArtifacts = await loadRequestArtifacts(root);
    const taskArtifacts = await loadTaskArtifacts(root);
    const report = buildReport(root, taxonomy, requestArtifacts, taskArtifacts, mappings);

    return Response.json(report);
  } catch (error) {
    return Response.json({ error: error instanceof Error ? error.message : 'Failed to build taxonomy migration report.' }, { status: 500 });
  }
}

export async function POST() {
  if (process.env.NODE_ENV === 'production') {
    return Response.json({ error: 'Not Found' }, { status: 404 });
  }

  try {
    const root = repoRoot();
    const taxonomy = await readCategoryTaxonomy(root);
    if (!taxonomy) {
      return Response.json({ error: 'Category taxonomy not found.' }, { status: 404 });
    }
    const mappings = buildDeprecatedMappings(taxonomy);
    const requestArtifacts = await loadRequestArtifacts(root);
    const taskArtifacts = await loadTaskArtifacts(root);
    const beforeReport = buildReport(root, taxonomy, requestArtifacts, taskArtifacts, mappings);

    const requestUpdates = await applyRequestMigrations(root, requestArtifacts, mappings);
    const taskUpdates = await applyTaskMigrations(root, taskArtifacts, mappings);
    const updatedFiles = [...requestUpdates, ...taskUpdates];
    const allRewrites = updatedFiles.flatMap((file) => file.rewrites);
    const afterReport = buildReport(root, taxonomy, requestArtifacts, taskArtifacts, mappings);

    const receipt: ResearchCategoryMigrationApplyReceipt = {
      appliedAtUtc: new Date().toISOString(),
      taxonomyVersion: taxonomy.taxonomyVersion,
      counts: {
        totalFilesUpdated: updatedFiles.length,
        requestFilesUpdated: requestUpdates.length,
        taskArtifactsUpdated: taskUpdates.length,
        categoriesRewritten: allRewrites.length,
        requestCategoriesRewritten: allRewrites.filter((rewrite) => rewrite.sourceType === 'request').length,
        taskItemCategoriesRewritten: allRewrites.filter((rewrite) => rewrite.sourceType === 'task-item').length,
        resolvedTaskItemCategoriesRewritten: allRewrites.filter((rewrite) => rewrite.sourceType === 'resolved-task-item').length,
      },
      updatedFiles,
    };

    await appendCategoryTaxonomyAuditEntry(root, {
      action: 'apply-migration-fixup',
      taxonomyVersion: taxonomy.taxonomyVersion,
      summary: receipt.counts.totalFilesUpdated === 0
        ? 'Applied taxonomy migration fix-up with no changes.'
        : `Applied taxonomy migration fix-up to ${receipt.counts.totalFilesUpdated} files.`,
      beforeTaxonomy: taxonomy,
      afterTaxonomy: taxonomy,
      beforeMigrationReport: beforeReport,
      afterMigrationReport: afterReport,
      applyReceipt: receipt,
    });

    return Response.json(receipt);
  } catch (error) {
    return Response.json({ error: error instanceof Error ? error.message : 'Failed to apply taxonomy migration fix-up.' }, { status: 500 });
  }
}