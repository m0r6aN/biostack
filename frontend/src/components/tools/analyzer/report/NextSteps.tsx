'use client';

import Link from 'next/link';
import type { ProtocolAnalyzerResult } from '@/lib/types';

const ANALYZER_PRICING_HREF = '/pricing?intent=analyzer';

// ── NextSteps ─────────────────────────────────────────────────────────────────
// From monolith CTA section (~674-704) + save notice (~569-573).

export interface NextStepsProps {
  result: ProtocolAnalyzerResult;
  savedAnalysisId: string;
  showSaveNotice: boolean;
  isAuthenticated: boolean;
  hasProfile: boolean;
  showUpgrade: boolean;
  onSave: () => void;
  onConvert: () => void;
  onUnlockClicked: () => void;
}

export function NextSteps({
  result,
  savedAnalysisId,
  showSaveNotice,
  isAuthenticated,
  hasProfile,
  showUpgrade,
  onSave,
  onConvert,
  onUnlockClicked,
}: NextStepsProps) {
  // Determine the sign-in / profile nudge link based on auth state.
  const nudgeHref = !isAuthenticated
    ? '/auth/signin?callbackUrl=/tools/analyzer'
    : '/profiles';

  return (
    <section className="rounded-lg border border-white/[0.08] bg-[#121923]/95 p-4">
      <h2 className="text-lg font-semibold text-white">Track whether these patterns hold</h2>
      <p className="mt-3 text-sm leading-6 text-white/58">
        Save this stack as a protocol and check in over time to see whether the synergies and conflicts playing out now actually hold.
      </p>

      <div className="mt-4 flex flex-wrap gap-3">
        <button
          type="button"
          onClick={onSave}
          disabled={!result}
          className="rounded-lg border border-white/10 px-4 py-2 text-sm font-semibold text-white/72 hover:border-white/20 hover:text-white"
        >
          Save Analysis
        </button>
        <button
          type="button"
          onClick={onConvert}
          disabled={!result}
          className="rounded-lg bg-emerald-400 px-4 py-2 text-sm font-semibold text-slate-950 hover:bg-emerald-300 disabled:cursor-not-allowed disabled:opacity-60"
        >
          Convert to BioStack Protocol
        </button>
        {showUpgrade && (
          <Link
            href={ANALYZER_PRICING_HREF}
            onClick={onUnlockClicked}
            className="rounded-lg border border-white/10 px-4 py-2 text-sm font-semibold text-white/72 hover:border-white/20 hover:text-white"
          >
            View Operator access
          </Link>
        )}
      </div>

      {/* Save notice — copy from monolith ~570-572 */}
      {showSaveNotice && (
        <p className="mt-4 text-sm leading-6 text-emerald-100/80">
          Analysis saved locally{savedAnalysisId ? ` as ${savedAnalysisId}` : ''}. It will stay available through sign-in and protocol conversion.
        </p>
      )}

      {/* Profile nudge when auth is incomplete */}
      {(!isAuthenticated || !hasProfile) && (
        <p className="mt-4 text-sm leading-6 text-white/55">
          <Link href={nudgeHref} className="underline decoration-white/30 underline-offset-2 hover:text-white">
            Create a free profile
          </Link>{' '}
          to save this analysis and track how your stack changes over time. No card required.
        </p>
      )}
    </section>
  );
}
