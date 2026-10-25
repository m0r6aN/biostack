'use client';

import { useState } from 'react';

/** Voice guide §4 /vs share text: "[A] vs [B], the evidence ledger. biostack.cc/vs/a/b". */
export function vsShareText(a: string, b: string, path: string): string {
  return `${a} vs ${b}, the evidence ledger. biostack.cc${path}`;
}

export function VsShare({ a, b, path }: { a: string; b: string; path: string }) {
  const [done, setDone] = useState('');
  async function share() {
    const text = vsShareText(a, b, path);
    try {
      if (typeof navigator.share === 'function') {
        await navigator.share({ title: `${a} vs ${b} | BioStack`, text, url: `${window.location.origin}${path}` });
        setDone('Shared');
      } else {
        await navigator.clipboard.writeText(text);
        setDone('Link copied');
      }
    } catch {
      setDone('');
    }
  }
  return (
    <div className="flex flex-wrap gap-2">
      <button type="button" onClick={share} className="rounded-lg border border-white/20 px-3 py-1.5 text-xs font-medium text-white/85 hover:bg-white/10">
        {done || 'Settle it'}
      </button>
      <a href={`/og${path}?format=story`} download className="rounded-lg border border-white/15 px-3 py-1.5 text-xs text-white/70 hover:bg-white/10">Story card</a>
    </div>
  );
}
