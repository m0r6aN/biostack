'use client';

import { useState } from 'react';
import { cn } from '@/lib/utils';
import { RegulatoryBoundaryBadge } from '@/components/intel/RegulatoryBoundaryBadge';

const BOUNDARY_EXPLANATIONS: Record<string, string> = {
  'not-regulated':
    'Not subject to pharmaceutical regulation in the contexts reviewed.',
  'dietary-supplement':
    'Classified as a dietary supplement. Health claims are restricted.',
  'prescription-only':
    'Prescription required. BioStack does not provide medical advice.',
  'controlled-substance':
    'This compound is a controlled substance. Significant legal restrictions apply.',
  'food-ingredient':
    'Recognized as a food ingredient in most jurisdictions.',
};

interface RegulatoryBoundaryPanelProps {
  boundary: string;
  promotionBlockers?: string[];
}

export function RegulatoryBoundaryPanel({ boundary, promotionBlockers = [] }: RegulatoryBoundaryPanelProps) {
  const [blockersOpen, setBlockersOpen] = useState(false);
  const explanation = BOUNDARY_EXPLANATIONS[boundary.toLowerCase()] ?? boundary;

  return (
    <div className="space-y-3">
      <div className="flex items-start gap-3 p-4 rounded-xl border border-white/8 bg-white/[0.03]">
        <RegulatoryBoundaryBadge boundary={boundary} className="shrink-0 mt-0.5" />
        <p className="text-sm text-white/60 leading-relaxed">{explanation}</p>
      </div>

      {promotionBlockers.length > 0 && (
        <div className="rounded-xl border border-amber-400/15 bg-amber-500/[0.06] overflow-hidden">
          <button
            onClick={() => setBlockersOpen(p => !p)}
            className="w-full flex items-center justify-between px-4 py-3 text-left"
            aria-expanded={blockersOpen}
          >
            <span className="text-xs font-semibold text-amber-300 uppercase tracking-wider">
              Promotion Blockers ({promotionBlockers.length})
            </span>
            <svg
              className={cn('w-4 h-4 text-amber-400/60 transition-transform', blockersOpen && 'rotate-180')}
              fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.5}
            >
              <path strokeLinecap="round" strokeLinejoin="round" d="M19 9l-7 7-7-7" />
            </svg>
          </button>
          {blockersOpen && (
            <ul className="px-4 pb-4 space-y-2 border-t border-amber-400/10">
              {promotionBlockers.map((blocker, i) => (
                <li key={i} className="flex items-start gap-2 pt-2">
                  <span className="text-amber-400 shrink-0 mt-0.5">·</span>
                  <span className="text-sm text-amber-200/70">{blocker}</span>
                </li>
              ))}
            </ul>
          )}
        </div>
      )}
    </div>
  );
}
