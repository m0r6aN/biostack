'use client';

import Link from 'next/link';
import { OPERATING_STATE_TOKENS } from '@/styles/tokens';
import { deriveOperatingState, type OperatingStateResult } from '@/lib/derive/operatingState';
import { WhyDrawer } from '@/components/intel/WhyDrawer';
import { cn } from '@/lib/utils';
import { track } from '@/lib/telemetry';
import type { ProtocolConsolePayload, CompoundRecord } from '@/lib/types';

interface OperatingStateHeroProps {
  payload: ProtocolConsolePayload | null;
  compounds: CompoundRecord[];
  className?: string;
}

const STATE_ICON: Record<string, React.ReactNode> = {
  running: (
    <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="1.5">
      <path strokeLinecap="round" strokeLinejoin="round" d="M5.25 5.653c0-.856.917-1.398 1.667-.986l11.54 6.347a1.125 1.125 0 0 1 0 1.972l-11.54 6.347a1.125 1.125 0 0 1-1.667-.986V5.653Z" />
    </svg>
  ),
  'awaiting-first-observation': (
    <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="1.5">
      <path strokeLinecap="round" strokeLinejoin="round" d="M12 6v6h4.5m4.5 0a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z" />
    </svg>
  ),
  'review-pending': (
    <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="1.5">
      <path strokeLinecap="round" strokeLinejoin="round" d="M9 12h3.75M9 15h3.75M9 18h3.75m3 .75H18a2.25 2.25 0 0 0 2.25-2.25V6.108c0-1.135-.845-2.098-1.976-2.192a48.424 48.424 0 0 0-1.123-.08m-5.801 0c-.065.21-.1.433-.1.664 0 .414.336.75.75.75h4.5a.75.75 0 0 0 .75-.75 2.25 2.25 0 0 0-.1-.664m-5.8 0A2.251 2.251 0 0 1 13.5 2.25H15c1.012 0 1.867.668 2.15 1.586m-5.8 0c-.376.023-.75.05-1.124.08C9.095 4.01 8.25 4.973 8.25 6.108V8.25m0 0H4.875c-.621 0-1.125.504-1.125 1.125v11.25c0 .621.504 1.125 1.125 1.125h9.75c.621 0 1.125-.504 1.125-1.125V9.375c0-.621-.504-1.125-1.125-1.125H8.25ZM6.75 12h.008v.008H6.75V12Zm0 3h.008v.008H6.75V15Zm0 3h.008v.008H6.75V18Z" />
    </svg>
  ),
  'drift-accumulating': (
    <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="1.5">
      <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126ZM12 15.75h.007v.008H12v-.008Z" />
    </svg>
  ),
  'stable-baseline': (
    <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="1.5">
      <path strokeLinecap="round" strokeLinejoin="round" d="M3 13.125C3 12.504 3.504 12 4.125 12h2.25c.621 0 1.125.504 1.125 1.125v6.75C7.5 20.496 6.996 21 6.375 21h-2.25A1.125 1.125 0 0 1 3 19.875v-6.75ZM9.75 8.625c0-.621.504-1.125 1.125-1.125h2.25c.621 0 1.125.504 1.125 1.125v11.25c0 .621-.504 1.125-1.125 1.125h-2.25a1.125 1.125 0 0 1-1.125-1.125V8.625ZM16.5 4.125c0-.621.504-1.125 1.125-1.125h2.25C20.496 3 21 3.504 21 4.125v15.75c0 .621-.504 1.125-1.125 1.125h-2.25a1.125 1.125 0 0 1-1.125-1.125V4.125Z" />
    </svg>
  ),
  'no-active-run': (
    <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="1.5">
      <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m9-.75a9 9 0 1 1-18 0 9 9 0 0 1 18 0Zm-9 3.75h.008v.008H12v-.008Z" />
    </svg>
  ),
};

export function OperatingStateHero({ payload, compounds, className }: OperatingStateHeroProps) {
  const result: OperatingStateResult = deriveOperatingState(payload, compounds.filter((c) => c.status === 'Active').length);
  const t = OPERATING_STATE_TOKENS[result.state];

  return (
    <div className={cn('rounded-3xl border p-6', t.bg, t.border, className)}>
      <div className="flex items-start gap-4">
        {/* State icon */}
        <div className={cn('flex items-center justify-center w-11 h-11 rounded-2xl border shrink-0 mt-0.5', t.bg, t.border, t.color)}>
          {STATE_ICON[result.state]}
        </div>

        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2 mb-1">
            <h2 className={cn('text-lg font-bold leading-tight', t.color)}>{t.label}</h2>
            <WhyDrawer
              surface="Operating State"
              title="How is this state determined?"
              inputs={result.whyInputs}
              reasoning={result.reasoning}
              caveats={result.caveats}
            />
          </div>
          <p className="text-sm text-white/60 leading-relaxed">{t.sub}</p>

          {/* Active run metadata */}
          {result.activeRun && (
            <div className="mt-3 flex flex-wrap gap-x-4 gap-y-1">
              {result.activeRun.startedAtUtc && (
                <span className="text-[11px] text-white/35">
                  Started {new Date(result.activeRun.startedAtUtc).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })}
                </span>
              )}
              {result.activeRun.protocolName && (
                <span className="text-[11px] text-white/35">Run: {result.activeRun.protocolName}</span>
              )}
            </div>
          )}
        </div>

        {/* CTA */}
        <div className="shrink-0">
          <Link
            href={t.ctaHref}
            onClick={() => track({ name: 'operating_state_cta_click', state: result.state })}
            className={cn(
              'inline-flex items-center gap-2 px-4 py-2.5 rounded-2xl text-xs font-semibold border transition-all duration-200',
              t.bg, t.color, t.border,
              'hover:brightness-110 hover:shadow-lg',
            )}
          >
            {t.cta}
            <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="2">
              <path strokeLinecap="round" strokeLinejoin="round" d="M13.5 4.5 21 12m0 0-7.5 7.5M21 12H3" />
            </svg>
          </Link>
        </div>
      </div>
    </div>
  );
}
