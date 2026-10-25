import { EMBED_HEADERS, deniedHost, parseEmbedQuery, vsEmbedHtml } from '@/lib/embed';
import { fetchKnowledgeEntry } from '@/lib/knowledgeServer';
import { isCuratedPair, rulingFor, vsAsOf, vsPath, vsSide } from '@/lib/vsPairs';

// Growth Phase 8: /vs embed (360×220). Ruling only for curated pairs with two published verdicts.
export const revalidate = 600;

export async function GET(request: Request, { params }: { params: Promise<{ a: string; b: string }> }) {
  const { a, b } = await params;
  const { theme } = parseEmbedQuery(request.url);
  const [ea, eb] = await Promise.all([fetchKnowledgeEntry(a), fetchKnowledgeEntry(b)]);
  const left = ea ? vsSide(ea) : null;
  const right = eb ? vsSide(eb) : null;
  const ruling = left && right && isCuratedPair(a, b) ? rulingFor(left, right) : null;
  const html = vsEmbedHtml(left, right, ruling, vsPath(a, b), left && right ? vsAsOf(left, right) : '', theme, deniedHost(request.headers.get('referer')));
  return new Response(html, { status: left && right ? 200 : 404, headers: EMBED_HEADERS });
}
