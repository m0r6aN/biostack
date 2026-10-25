'use client';

import Link from 'next/link';
import { useEffect, useState } from 'react';
import { useOptionalAuth } from '@/lib/AuthProvider';
import { watchlistApi, type WatchTargetType } from '@/lib/watchlist';

export const WATCH_HELP = 'Get an email when the verdict changes or a reviewed study is added. No dosing, ever.';

/** One-tap watch for a compound, claim or /vs pair (03 §6). Signed-out visitors get a sign-in link. */
export function WatchButton({ targetType, targetId, returnTo }: { targetType: WatchTargetType; targetId: string; returnTo: string }) {
  const auth = useOptionalAuth();
  const [watchId, setWatchId] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState(false);
  const signedIn = Boolean(auth?.user);
  const id = targetId.toLowerCase();

  useEffect(() => {
    if (!signedIn) return;
    let live = true;
    watchlistApi.list()
      .then(ws => { if (live) setWatchId(ws.find(w => w.targetType === targetType && w.targetId === id)?.id ?? null); })
      .catch(() => {});
    return () => { live = false; };
  }, [signedIn, targetType, id]);

  if (!signedIn) {
    return (
      <Link href={`/auth/signin?callbackUrl=${encodeURIComponent(returnTo)}`} title={WATCH_HELP}
        className="inline-flex items-center rounded-lg border border-white/15 px-3 py-1.5 text-sm text-white/80 hover:bg-white/5">
        Sign in to watch
      </Link>
    );
  }

  const toggle = async () => {
    setBusy(true); setError(false);
    try {
      if (watchId) { await watchlistApi.remove(watchId); setWatchId(null); }
      else setWatchId((await watchlistApi.add(targetType, id)).id);
    } catch { setError(true); } finally { setBusy(false); }
  };

  return (
    <span className="inline-flex items-center gap-2">
      <button type="button" onClick={toggle} disabled={busy} aria-pressed={Boolean(watchId)} title={WATCH_HELP}
        className="inline-flex items-center rounded-lg border border-cyan-300/30 px-3 py-1.5 text-sm text-cyan-100 hover:bg-cyan-400/10 disabled:opacity-60">
        {watchId ? 'Watching' : 'Watch for changes'}
      </button>
      {error && <span role="alert" className="text-xs text-amber-300">Couldn&rsquo;t update. Try again.</span>}
    </span>
  );
}
