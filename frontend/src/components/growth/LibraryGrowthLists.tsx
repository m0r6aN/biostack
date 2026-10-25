'use client';

import { useEffect, useState } from 'react';
import type { GrowthFeed } from '@/lib/growthFeed';
import { GrowthLists } from './GrowthLists';

/** /knowledge (client page): the same lists from the cached /growth-feed route. */
export function LibraryGrowthLists() {
  const [feed, setFeed] = useState<GrowthFeed | null>(null);
  useEffect(() => {
    let live = true;
    fetch('/growth-feed').then(r => (r.ok ? r.json() : null)).then(d => { if (live && d) setFeed(d as GrowthFeed); }).catch(() => {});
    return () => { live = false; };
  }, []);
  if (!feed || (!feed.today && !feed.myth && feed.hypeGap.length === 0 && feed.trending.length === 0)) return null;
  return <GrowthLists feed={{ ...feed, headToHead: [] }} showSearch={false} />;
}
