'use client';

import Link from 'next/link';

/**
 * Route-level error boundary. Keeps the recovery path and the support link on screen when a page throws,
 * instead of the framework's bare error view. The error detail is never shown to the reader.
 */
export default function RouteError({ reset }: { error: Error & { digest?: string }; reset: () => void }) {
  return (
    <main id="main" tabIndex={-1} className="mx-auto max-w-3xl px-5 py-20 sm:px-8" style={{ position: 'relative', zIndex: 1 }}>
      <p className="text-xs font-semibold uppercase tracking-[0.3em] text-emerald-300/70">Something went wrong</p>
      <h1 className="mt-4 text-4xl font-semibold tracking-tight text-white sm:text-5xl">This page did not load.</h1>
      <p className="mt-5 text-lg leading-8 text-white/62">
        Something broke while loading it. Try again, or go back to the start.
        If it keeps happening, the support page explains how to reach us.
      </p>
      <div className="mt-8 flex flex-wrap gap-3">
        <button
          type="button"
          onClick={() => reset()}
          className="min-h-11 rounded-lg bg-emerald-400 px-5 text-sm font-semibold text-slate-950 transition-colors hover:bg-emerald-300"
        >
          Try again
        </button>
        <Link href="/" className="inline-flex min-h-11 items-center rounded-lg border border-white/12 px-5 text-sm font-semibold text-white transition-colors hover:border-white/24">
          Back to home
        </Link>
        <Link href="/support" className="inline-flex min-h-11 items-center rounded-lg border border-white/12 px-5 text-sm font-semibold text-white transition-colors hover:border-white/24">
          Get help
        </Link>
      </div>
    </main>
  );
}
