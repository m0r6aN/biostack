import { NextRequest } from 'next/server';
import { readFile } from 'fs/promises';
import path from 'path';

const ALLOWED: Record<string, string> = {
  'research-summary': 'research-summary.json',
  'promotion-manifest': 'promotion-manifest.json',
  'review-resolution-plan': 'review-resolution-plan.json',
  'promotion-import-preview': 'promotion-import-preview.json',
  'import-dry-run/promotion-import-dry-run-report':
    'import-dry-run/promotion-import-dry-run-report.json',
  'promotion-export/promotion-export-manifest':
    'promotion-export/promotion-export-manifest.json',
  'promotion-export/substances.promotable':
    'promotion-export/substances.promotable.json',
};

export async function GET(request: NextRequest) {
  // Route is dev-only; return 404 in production
  if (process.env.NODE_ENV === 'production') {
    return Response.json({ error: 'Not Found' }, { status: 404 });
  }

  // Extract and validate artifact parameter
  const artifact = request.nextUrl.searchParams.get('artifact') ?? '';
  const filename = ALLOWED[artifact];
  if (!filename) {
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

  let basePath: string;
  if (dataSource === 'api') {
    const artifactsPath = process.env.RESEARCH_ARTIFACTS_PATH ?? 'research/pilot';
    // Anchor to the Next.js app root (cwd) parent — this is the monorepo root
    const repoRoot = path.resolve(process.cwd(), '..');
    const resolved = path.resolve(repoRoot, artifactsPath);
    // Append sep to prevent prefix-sibling bypass (e.g. BioStack-evil vs BioStack)
    const repoRootWithSep = repoRoot.endsWith(path.sep) ? repoRoot : repoRoot + path.sep;
    if (!resolved.startsWith(repoRootWithSep) && resolved !== repoRoot) {
      return Response.json({ error: 'Invalid path' }, { status: 400 });
    }
    basePath = resolved;
  } else {
    basePath = path.resolve(process.cwd(), 'src/fixtures/research');
  }

  // Read and return the artifact
  try {
    const content = await readFile(path.join(basePath, filename), 'utf-8');
    return Response.json(JSON.parse(content));
  } catch {
    return Response.json({ error: 'Artifact not found' }, { status: 404 });
  }
}
