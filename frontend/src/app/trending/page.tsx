import { GrowthLists } from '@/components/growth/GrowthLists';
import { buildGrowthFeed } from '@/lib/growthFeed';
import { createPublicPageMetadata } from '@/lib/site';

export const revalidate = 3600;

export const metadata = createPublicPageMetadata({
  title: 'Trending and biggest Hype Gaps | BioStack',
  description: 'What people are looking up on BioStack this week, and the graded claims with the biggest gap between talk and evidence.',
  path: '/trending',
});

export default async function TrendingPage() {
  const feed = await buildGrowthFeed();
  const empty = feed.hypeGap.length === 0 && feed.trending.length === 0;
  return (
    <main className="max-w-3xl mx-auto py-10 space-y-4">
      <h1 className="px-5 sm:px-8 text-2xl font-semibold text-white">Trending</h1>
      {empty ? <p className="px-5 sm:px-8 text-sm text-white/55">Nothing to rank yet. Lists appear once verdicts are published.</p>
        : <GrowthLists feed={{ ...feed, today: null, myth: null, headToHead: [] }} showSearch={false} />}
    </main>
  );
}
