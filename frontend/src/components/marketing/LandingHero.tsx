'use client';

import Link from 'next/link';
import { useState } from 'react';
import { StackIntelligencePanel } from '@/components/marketing/StackIntelligencePanel';
import { cn } from '@/lib/utils';

type HeroMode = 'new' | 'existing';

const heroModes = {
  new: {
    bridge: 'See how this works before you commit to anything',
    headline: 'Not Sure Where to Start?',
    subheadline:
      "BioStack helps you track what you're taking, understand how things interact, and avoid common mistakes from day one.",
    microcopy: 'Start from scratch or add what you already have',
    support: 'Get organized early, catch overlap sooner, and build with more confidence.',
  },
  existing: {
    bridge: 'See this with your own stack',
    headline: 'See How Your Stack Works Together',
    subheadline:
      "Add what you're taking and BioStack shows what overlaps, what’s unnecessary, and what actually makes sense together.",
    microcopy: 'Add 2–3 things and see how they connect',
    support: 'See what overlaps, what may be unnecessary, and what could work better together.',
  },
} as const;

export function LandingHero() {
  const [mode, setMode] = useState<HeroMode>('new');
  const content = heroModes[mode];

  return (
    <section className="min-h-[calc(100svh-73px)] border-b border-white/8">
      <div className="mx-auto grid max-w-7xl gap-6 px-5 py-8 sm:px-8 sm:py-10 lg:grid-cols-[1.05fr_0.95fr] lg:items-start lg:gap-8 lg:py-12">
        <div className="max-w-2xl">
          <p className="text-xs font-semibold uppercase tracking-[0.34em] text-emerald-300/72">
            Protocol Intelligence
          </p>

          <div className="mt-5 inline-flex rounded-full border border-white/10 bg-white/[0.03] p-1">
            {[
              { id: 'new', label: 'New to this' },
              { id: 'existing', label: 'Already have a stack' },
            ].map((option) => {
              const isActive = mode === option.id;

              return (
                <button
                  key={option.id}
                  type="button"
                  aria-pressed={isActive}
                  onClick={() => setMode(option.id as HeroMode)}
                  className={cn(
                    'rounded-full px-4 py-2 text-sm font-medium transition-colors',
                    isActive ? 'bg-emerald-300 text-slate-950' : 'text-white/65 hover:text-white'
                  )}
                >
                  {option.label}
                </button>
              );
            })}
          </div>

          <h1 className="mt-6 text-4xl font-semibold leading-[0.96] tracking-tight text-white sm:text-5xl lg:text-5xl xl:text-6xl">
            {content.headline}
          </h1>
          <p className="mt-5 max-w-xl text-base leading-7 text-white/62 sm:text-lg sm:leading-8">
            {content.subheadline}
          </p>

          <p className="mt-6 text-sm font-medium text-emerald-200/80">{content.bridge}</p>

          <div className="mt-4 flex flex-wrap gap-3">
            <Link
              href="/onboarding"
              className="rounded-full bg-emerald-400 px-6 py-3 text-sm font-semibold text-slate-950 transition-transform hover:-translate-y-0.5"
            >
              Start Free
            </Link>
            <Link
              href="/tools/reconstitution-calculator"
              className="rounded-full border border-white/12 px-6 py-3 text-sm font-semibold text-white transition-colors hover:border-white/24"
            >
              Try the Calculator First
            </Link>
          </div>

          <div className="mt-4 space-y-1 text-sm text-white/52">
            <p>{content.microcopy}</p>
            <p>Takes less than a minute. No complicated setup.</p>
          </div>

          <p className="mt-4 max-w-xl text-sm text-white/58">{content.support}</p>
        </div>

        <StackIntelligencePanel className="lg:mt-1" />
      </div>
    </section>
  );
}