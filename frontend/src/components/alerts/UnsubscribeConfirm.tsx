'use client';

import Link from 'next/link';
import { useState } from 'react';
import { watchlistApi } from '@/lib/watchlist';

// A button, not an automatic GET: link scanners prefetch email links. Mail apps use the
// List-Unsubscribe-Post header, which POSTs straight to the API.
export function UnsubscribeConfirm({ token }: { token: string }) {
  const [state, setState] = useState<'idle' | 'done' | 'error'>('idle');
  if (state === 'done') {
    return (
      <p className="text-white/75">
        Done. You won&rsquo;t get alert emails. Your watchlist and in-app notices stay. <Link href="/account/alerts" className="text-cyan-300 underline">Alert settings</Link>
      </p>
    );
  }
  return (
    <div className="space-y-3">
      <p className="text-white/70">One click turns off every evidence alert email for this account.</p>
      <button type="button" onClick={() => watchlistApi.unsubscribe(token).then(() => setState('done'), () => setState('error'))}
        className="rounded-lg border border-cyan-300/30 px-4 py-2 text-sm text-cyan-100 hover:bg-cyan-400/10">
        Stop alert emails
      </button>
      {state === 'error' && <p role="alert" className="text-sm text-amber-300">That didn&rsquo;t go through. Try again.</p>}
    </div>
  );
}
