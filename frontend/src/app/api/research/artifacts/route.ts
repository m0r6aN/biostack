import { readFile } from 'fs/promises';
import { NextRequest } from 'next/server';
import path from 'path';

type ArtifactScope = 'repo' | 'data-source';

const ALLOWED: Record<string, { filename: string; scope: ArtifactScope }> = {
  'category-taxonomy': { filename: path.join('research', 'category-taxonomy.json'), scope: 'repo' },
  'research-summary': { filename: 'research-summary.json', scope: 'data-source' },
  'research-task-queue': { filename: 'research-task-queue.json', scope: 'data-source' },
  'promotion-manifest': { filename: 'promotion-manifest.json', scope: 'data-source' },
  'review-queue': { filename: 'review-queue.json', scope: 'data-source' },
  'review-resolution-plan': { filename: 'review-resolution-plan.json', scope: 'data-source' },
  'promotion-import-preview': { filename: 'promotion-import-preview.json', scope: 'data-source' },
  'import-dry-run/promotion-import-dry-run-report': {
    filename: 'import-dry-run/promotion-import-dry-run-report.json',
    scope: 'data-source',
  },
  'promotion-export/promotion-export-manifest': {
    filename: 'promotion-export/promotion-export-manifest.json',
    scope: 'data-source',
  },
  'promotion-export/substances.promotable': {
    filename: 'promotion-export/substances.promotable.json',
    scope: 'data-source',
  },
};

const EVIDENCE_PACKET_PATTERN = /^evidence-packet\/[a-z0-9]+(?:-[a-z0-9]+)*$/;

function repoRoot() {
  return path.resolve(process.cwd(), '..');
}

function resolveArtifactFilename(artifact: string): { filename: string; scope: ArtifactScope } | null {
  if (EVIDENCE_PACKET_PATTERN.test(artifact)) {
    const slug = artifact.slice('evidence-packet/'.length);
    return { filename: `evidence-packet/${slug}.json`, scope: 'data-source' };
  }

  return ALLOWED[artifact] ?? null;
}

export async function GET(request: NextRequest) {
  // Route is dev-only; return 404 in production
  if (process.env.NODE_ENV === 'production') {
    return Response.json({ error: 'Not Found' }, { status: 404 });
  }

  // Extract and validate artifact parameter
  const artifact = request.nextUrl.searchParams.get('artifact') ?? '';
  const resolvedArtifact = resolveArtifactFilename(artifact);
  if (!resolvedArtifact) {
    return Response.json({ error: 'Invalid artifact' }, { status: 400 });
  }

  // Determine base path based on data source
  const dataSource = process.env.RESEARCH_DATA_SOURCE ?? 'fixtures';

  // Auth is required when serving real artifacts (api mode) — fixture data is not sensitive
  // and the route is already dev-only (returns 404 in production above).
  if (dataSource !== 'fixtures') {
    const auth = request.headers.get('authorization');
    if (!auth?.startsWith('Bearer ')) {
      return Response.json({ error: 'Unauthorized' }, { status: 401 });
    }
  }

  let filePath: string;
  if (resolvedArtifact.scope === 'repo') {
    filePath = path.resolve(repoRoot(), resolvedArtifact.filename);
  } else {
    let basePath: string;
    if (dataSource === 'api') {
      const artifactsPath = process.env.RESEARCH_ARTIFACTS_PATH ?? 'research/pilot';
      const root = repoRoot();
      const resolved = path.resolve(root, artifactsPath);
      const repoRootWithSep = root.endsWith(path.sep) ? root : root + path.sep;
      if (!resolved.startsWith(repoRootWithSep) && resolved !== root) {
        return Response.json({ error: 'Invalid path' }, { status: 400 });
      }
      basePath = resolved;
    } else {
      basePath = path.resolve(process.cwd(), 'src/fixtures/research');
    }
    filePath = path.join(basePath, resolvedArtifact.filename);
  }

  // Read and return the artifact
  try {
    const content = await readFile(filePath, 'utf-8');
    return Response.json(JSON.parse(content));
  } catch {
    return Response.json({ error: 'Artifact not found' }, { status: 404 });
  }
}
