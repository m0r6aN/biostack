'use client';

import { useEffect, useId, useRef, useState } from 'react';
import { helpTips, type HelpTipKey } from '@/lib/helpTips';
import { cn } from '@/lib/utils';

interface HelpTipProps {
  tipKey: HelpTipKey;
  children: React.ReactNode;
  className?: string;
}

const DISPLAY_LABELS: Record<HelpTipKey, string> = {
  evidenceTier:        'Evidence Tier',
  synergy:             'Synergy',
  redundancy:          'Redundancy',
  interference:        'Interference',
  communitySignal:     'Community Signal',
  reviewRequired:      'Review Required',
  counterfactual:      'Counterfactual',
  pathwayOverlap:      'Pathway Overlap',
  mechanisticEvidence: 'Mechanistic Evidence',
};

export function HelpTip({ tipKey, children, className }: HelpTipProps) {
  const [open, setOpen] = useState(false);
  const triggerRef = useRef<HTMLSpanElement>(null);
  const panelRef = useRef<HTMLDivElement>(null);
  const rawId = useId();
  const panelId = `helptip-${rawId.replace(/:/g, '')}`;

  useEffect(() => {
    if (!open) return;

    function handleKeyDown(e: KeyboardEvent) {
      if (e.key === 'Escape') setOpen(false);
    }
    function handleMouseDown(e: MouseEvent) {
      if (
        triggerRef.current && !triggerRef.current.contains(e.target as Node) &&
        panelRef.current && !panelRef.current.contains(e.target as Node)
      ) {
        setOpen(false);
      }
    }

    document.addEventListener('keydown', handleKeyDown);
    document.addEventListener('mousedown', handleMouseDown);
    return () => {
      document.removeEventListener('keydown', handleKeyDown);
      document.removeEventListener('mousedown', handleMouseDown);
    };
  }, [open]);

  return (
    <>
      <style>{`
        .htip:hover > .htip-text,
        .htip:focus-visible > .htip-text {
          text-shadow: 0 0 8px rgba(148,163,184,0.55);
        }
        @media (prefers-reduced-motion: reduce) {
          .htip > .htip-text { text-shadow: none !important; }
        }
      `}</style>
      <span
        ref={triggerRef}
        role="button"
        tabIndex={0}
        aria-expanded={open}
        aria-describedby={open ? panelId : undefined}
        onClick={() => setOpen(v => !v)}
        onKeyDown={e => {
          if (e.key === 'Enter' || e.key === ' ') {
            e.preventDefault();
            setOpen(v => !v);
          }
        }}
        className={cn('htip relative inline-block cursor-help', className)}
      >
        <span className="htip-text underline decoration-dotted underline-offset-2 decoration-white/25">
          {children}
        </span>

        {open && (
          <div
            ref={panelRef}
            id={panelId}
            role="tooltip"
            className="absolute bottom-[calc(100%+8px)] left-0 z-50 w-56 rounded-lg border border-white/10 bg-[#1e293b] p-3 shadow-[0_8px_24px_rgba(0,0,0,0.4)]"
          >
            <p className="mb-1.5 text-[10px] font-bold uppercase tracking-[0.1em] text-white/40">
              {DISPLAY_LABELS[tipKey]}
            </p>
            <p className="text-xs leading-relaxed text-white/70">
              {helpTips[tipKey]}
            </p>
            {/* CSS caret arrow */}
            <div className="absolute -bottom-[5px] left-3 h-2 w-2 rotate-45 border-b border-r border-white/10 bg-[#1e293b]" />
          </div>
        )}
      </span>
    </>
  );
}
