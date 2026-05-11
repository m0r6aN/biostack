'use client';

import { useState } from 'react';
import { cn } from '@/lib/utils';
import { ConfidenceChip } from '@/components/intel/ConfidenceChip';
import { TrustLedgerClaim } from '@/lib/types';

interface ClaimInspectorProps {
  claims: TrustLedgerClaim[];
}

export function ClaimInspector({ claims }: ClaimInspectorProps) {
  const [expandedIndex, setExpandedIndex] = useState<number | null>(null);

  if (claims.length === 0) {
    return (
      <p className="text-sm text-white/40 py-4">No claims available for this compound.</p>
    );
  }

  const toggle = (i: number) => setExpandedIndex(prev => (prev === i ? null : i));

  return (
    <div className="space-y-2">
      {claims.map((claim, i) => {
        const isOpen = expandedIndex === i;
        return (
          <div
            key={i}
            className={cn(
              'rounded-xl border transition-colors',
              isOpen
                ? 'border-white/15 bg-white/[0.06]'
                : 'border-white/8 bg-white/[0.03] hover:bg-white/[0.05]',
            )}
          >
            <button
              onClick={() => toggle(i)}
              className="w-full text-left px-4 py-3 flex items-start gap-3"
              aria-expanded={isOpen}
            >
              <span className="shrink-0 mt-0.5">
                <ConfidenceChip level={claim.confidence} showLabel={false} size="sm" />
              </span>
              <span className={cn('flex-1 text-sm leading-snug line-clamp-2', isOpen ? 'text-white/90' : 'text-white/70')}>
                {claim.claimText}
              </span>
              <span className="shrink-0 flex items-center gap-2 ml-2">
                {claim.reviewFlags.length > 0 && (
                  <span className="text-[10px] font-medium px-1.5 py-0.5 rounded bg-amber-500/15 text-amber-300 border border-amber-400/20">
                    {claim.reviewFlags.length} flag{claim.reviewFlags.length !== 1 ? 's' : ''}
                  </span>
                )}
                <svg
                  className={cn('w-4 h-4 text-white/30 transition-transform', isOpen && 'rotate-180')}
                  fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.5}
                >
                  <path strokeLinecap="round" strokeLinejoin="round" d="M19 9l-7 7-7-7" />
                </svg>
              </span>
            </button>

            {isOpen && (
              <div className="px-4 pb-4 space-y-4 border-t border-white/8 pt-3">
                <p className="text-sm text-white/80 leading-relaxed">{claim.claimText}</p>

                {claim.sourceRefs.length > 0 && (
                  <div>
                    <p className="text-[10px] font-bold text-white/30 uppercase tracking-widest mb-2">
                      Source References
                    </p>
                    <ul className="space-y-1">
                      {claim.sourceRefs.map((ref, j) => (
                        <li key={j} className="font-mono text-[11px] text-white/50 break-all">
                          {ref}
                        </li>
                      ))}
                    </ul>
                  </div>
                )}

                {claim.extractedQuote && (
                  <div>
                    <p className="text-[10px] font-bold text-white/30 uppercase tracking-widest mb-2">
                      Extracted Quote
                    </p>
                    <blockquote className="border-l-2 border-blue-400/40 pl-3 text-sm text-blue-200/70 italic leading-relaxed">
                      {claim.extractedQuote}
                    </blockquote>
                  </div>
                )}

                {claim.reviewFlags.length > 0 && (
                  <div>
                    <p className="text-[10px] font-bold text-white/30 uppercase tracking-widest mb-2">
                      Review Flags
                    </p>
                    <div className="flex flex-wrap gap-1.5">
                      {claim.reviewFlags.map((flag, j) => (
                        <span
                          key={j}
                          className="text-[11px] font-medium px-2 py-0.5 rounded-full bg-amber-500/10 text-amber-300 border border-amber-400/20"
                        >
                          {flag}
                        </span>
                      ))}
                    </div>
                  </div>
                )}
              </div>
            )}
          </div>
        );
      })}
    </div>
  );
}
