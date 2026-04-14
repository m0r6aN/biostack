'use client';

import Link from 'next/link';
import { StackIntelligencePanel } from '@/components/marketing/StackIntelligencePanel';

export function LandingHero() {
  return (
    <section className="min-h-[calc(100svh-73px)] border-b border-white/8">
      <div className="mx-auto grid max-w-7xl gap-11 px-5 py-11 sm:px-8 sm:py-12 lg:grid-cols-[0.92fr_1.08fr] lg:items-start lg:gap-10 lg:py-12">
        <div className="max-w-2xl">
          <p className="text-xs font-semibold uppercase tracking-[0.34em] text-emerald-300/72">
            Protocol Intelligence
          </p>

          <p className="mt-7 text-base font-semibold leading-7 text-white sm:text-lg">
            Stop guessing where to start - or what your stack is actually doing.
          </p>

          <h1 className="mt-4 text-4xl font-semibold leading-[0.96] tracking-tight text-white sm:text-5xl lg:text-5xl xl:text-6xl">
            BioStack Protocol Console
          </h1>
          <p className="mt-5 max-w-xl text-base leading-7 text-white/62 sm:text-lg sm:leading-8">
            Add what you&apos;re using - or thinking about using. BioStack shows how it fits,
            what overlaps, and what actually works together.
          </p>

          <div className="mt-4 grid gap-3 sm:grid-cols-[max-content_max-content]">
            <Link
              href="/onboarding"
              className="rounded-lg bg-emerald-400 px-5 py-3 text-sm font-semibold text-slate-950 transition-transform hover:-translate-y-0.5"
            >
              <span className="block">Build My Protocol</span>
              <span className="mt-1 block text-xs font-medium text-slate-950/70">Start with one compound or build a full stack.</span>
            </Link>
            <Link
              href="/onboarding?mode=existing"
              className="rounded-lg border border-white/12 px-5 py-3 text-sm font-semibold text-white transition-colors hover:border-white/24"
            >
              <span className="block">Map My Current Stack</span>
              <span className="mt-1 block text-xs font-medium text-white/48">See how your current compounds fit together.</span>
            </Link>
          </div>

          <p className="mt-4 max-w-xl text-sm font-medium leading-6 text-emerald-200/78">
            Some compounds overlap. Some work better together. BioStack shows the difference.
          </p>

          <p className="mt-3 max-w-xl text-sm leading-6 text-white/52">
            Conflicting advice, overlapping compounds, and guesswork make this harder than it should be.
          </p>

          <Link
            href="/tools"
            className="mt-5 inline-flex text-sm font-semibold text-emerald-200/70 transition-colors hover:text-white"
          >
            Explore Calculators
          </Link>
        </div>

        <StackIntelligencePanel className="mt-1 lg:mt-1" showModeToggle={false} />
      </div>
    </section>
  );
}
