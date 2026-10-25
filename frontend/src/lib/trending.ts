/**
 * "Most looked-up on BioStack this week" (03 §5): unique visitors per compound over 7 days
 * from first-party analytics. BioStack has no page-view store yet, so the feed is behind
 * config: TRENDING_FEED_URL returns [{ slug, uniqueVisitors7d }]. Unset → the list is hidden.
 * Never ranked by "most used".
 */
export interface TrendingItem { slug: string; uniqueVisitors7d: number }

export async function fetchTrending(limit = 5): Promise<TrendingItem[]> {
  const url = process.env.TRENDING_FEED_URL;
  if (!url) return [];
  try {
    const res = await fetch(url, { next: { revalidate: 3600 }, signal: AbortSignal.timeout(5000) });
    if (!res.ok) return [];
    const data = (await res.json()) as TrendingItem[];
    return Array.isArray(data)
      ? data.filter(d => typeof d.slug === 'string' && Number.isFinite(d.uniqueVisitors7d))
        .sort((a, b) => b.uniqueVisitors7d - a.uniqueVisitors7d).slice(0, limit)
      : [];
  } catch {
    return [];
  }
}
