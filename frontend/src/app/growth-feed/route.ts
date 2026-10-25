import { NextResponse } from 'next/server';
import { buildGrowthFeed } from '@/lib/growthFeed';

// Small cached feed for client pages (/knowledge) so they don't download the whole library.
export const revalidate = 3600;

export async function GET() {
  return NextResponse.json(await buildGrowthFeed(), {
    headers: { 'Cache-Control': 'public, max-age=600, s-maxage=3600' },
  });
}
