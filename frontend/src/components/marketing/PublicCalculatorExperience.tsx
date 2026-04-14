'use client';

import Link from 'next/link';
import { type FormEvent, useState } from 'react';
import { UnifiedDosingCalculator } from '@/components/calculators/UnifiedDosingCalculator';
import { apiClient } from '@/lib/api';

type CalculatorKind = 'reconstitution' | 'volume' | 'conversion';

interface PublicCalculatorExperienceProps {
  kind: CalculatorKind;
}

export function PublicCalculatorExperience({ kind }: PublicCalculatorExperienceProps) {
  const [email, setEmail] = useState('');
  const [leadState, setLeadState] = useState<'idle' | 'saving' | 'saved' | 'error'>('idle');

  async function handleLeadCapture(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!email.trim()) {
      return;
    }

    try {
      setLeadState('saving');
      await apiClient.captureLead(email.trim(), publicSource(kind));
      setLeadState('saved');
    } catch {
      setLeadState('error');
    }
  }

  return (
    <section className="mx-auto grid max-w-7xl gap-10 px-5 py-14 sm:px-8 lg:grid-cols-[0.78fr_1.22fr] lg:py-20">
      <div className="space-y-8">
        <div className="space-y-4">
          <p className="text-xs font-semibold uppercase tracking-[0.32em] text-emerald-300/70">
            Precision Tools
          </p>
          <h1 className="max-w-3xl text-4xl font-semibold tracking-tight text-white sm:text-5xl">
            Reconstitution &amp; Dosing Calculator
          </h1>
          <p className="max-w-2xl text-lg leading-8 text-white/62">
            Powder amount, diluent volume, desired dose, concentration, unit conversion, and daily or weekly split math in one calculator.
          </p>
        </div>

        <div className="grid gap-5 border-y border-white/8 py-6 text-sm text-white/55">
          <div>
            <p className="mb-1 text-white/90">One working surface</p>
            <p>Calculate concentration and draw volume without moving between separate tools.</p>
          </div>
          <div>
            <p className="mb-1 text-white/90">Daily and weekly splits</p>
            <p>Start with a per-dose amount, daily total, or weekly total and split it cleanly.</p>
          </div>
          <div>
            <p className="mb-1 text-white/90">Research boundary</p>
            <p>Calculated result only. Verify against your source material before use.</p>
          </div>
        </div>

        <div className="rounded-lg border border-white/10 bg-white/[0.03] p-5">
          <p className="text-sm text-white/72">
            Save the calculator link and get the Reconstitution &amp; Dosing Reference Card.
          </p>
          <form onSubmit={handleLeadCapture} className="mt-4 flex flex-col gap-3">
            <input
              type="email"
              value={email}
              onChange={(event) => setEmail(event.target.value)}
              placeholder="you@example.com"
              className="min-w-0 rounded-lg border border-white/12 bg-black/20 px-4 py-3 text-sm text-white outline-none transition-colors placeholder:text-white/30 focus:border-emerald-400/40"
            />
            <button
              type="submit"
              disabled={leadState === 'saving'}
              className="rounded-lg border border-white/12 px-5 py-3 text-sm font-medium text-white transition-colors hover:border-white/24 hover:text-emerald-200 disabled:opacity-60"
            >
              {leadState === 'saving' ? 'Saving...' : 'Send Reference Card'}
            </button>
          </form>

          {leadState === 'saved' && (
            <p className="mt-3 text-sm text-emerald-200/80">
              Saved. Your next step is creating a free account so calculations stay attached to your protocol.
            </p>
          )}

          {leadState === 'error' && (
            <p className="mt-3 text-sm text-amber-200/80">
              Lead capture failed. The calculator still works, and you can continue without saving.
            </p>
          )}
        </div>

        <div className="flex flex-wrap items-center gap-3 text-sm text-white/55">
          <Link href="/auth/signin" className="text-emerald-200 transition-colors hover:text-white">
            Create free account
          </Link>
          <span className="text-white/20">/</span>
          <Link href="/pricing" className="transition-colors hover:text-white">
            See pricing
          </Link>
        </div>
      </div>

      <UnifiedDosingCalculator />
    </section>
  );
}

function publicSource(kind: CalculatorKind) {
  if (kind === 'volume') return 'public-volume-calculator';
  if (kind === 'conversion') return 'public-unit-converter';
  return 'public-reconstitution-calculator';
}
