import { fetchAllEntries, findClaim } from '@/lib/claimLibrary';
import { EMBED_HEADERS, deniedHost, parseEmbedQuery, verdictEmbedHtml } from '@/lib/embed';

// Growth Phase 8: live "BioStack says" iframe (360×140 compact, 360×260 full).
export const revalidate = 600;

export async function GET(request: Request, { params }: { params: Promise<{ claimId: string }> }) {
  const { claimId } = await params;
  const { size, theme } = parseEmbedQuery(request.url);
  const hit = /^[A-Za-z0-9._-]{1,200}$/.test(claimId) ? findClaim(await fetchAllEntries(), claimId) : null;
  const html = verdictEmbedHtml({
    compound: hit?.entry.canonicalName ?? '', slug: hit?.slug ?? '', claim: hit?.claim ?? null,
    size, theme, denied: deniedHost(request.headers.get('referer')),
  });
  return new Response(html, { status: hit ? 200 : 404, headers: EMBED_HEADERS });
}
