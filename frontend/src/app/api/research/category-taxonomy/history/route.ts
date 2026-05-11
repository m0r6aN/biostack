import { readCategoryTaxonomyAuditLog, repoRoot } from '@/lib/research/categoryTaxonomyStorage';

export const runtime = 'nodejs';

export async function GET() {
  if (process.env.NODE_ENV === 'production') {
    return Response.json({ error: 'Not Found' }, { status: 404 });
  }

  try {
    return Response.json(await readCategoryTaxonomyAuditLog(repoRoot()));
  } catch (error) {
    return Response.json({ error: error instanceof Error ? error.message : 'Failed to load taxonomy audit history.' }, { status: 500 });
  }
}