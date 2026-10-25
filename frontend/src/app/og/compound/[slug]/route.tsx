import { renderCard } from '@/lib/cardResponse';
import { fetchKnowledgeEntry } from '@/lib/knowledgeServer';

// Growth Phase 3: compound verdict card (headline claim). 1200×630, or 1080×1920 with
// ?format=story. Cache key = path + ?v={claim version} + format, so a new version regenerates.
export const revalidate = 3600;

export async function GET(request: Request, { params }: { params: Promise<{ slug: string }> }) {
  const { slug } = await params;
  const entry = await fetchKnowledgeEntry(slug);
  return renderCard({ kind: 'compound', entry, request, fallbackName: 'Compound dossier' });
}
