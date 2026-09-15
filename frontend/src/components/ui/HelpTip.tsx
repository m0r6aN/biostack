'use client';

import { useEffect, useId, useLayoutEffect, useRef, useState } from 'react';
import { createPortal } from 'react-dom';
import { helpTips, type HelpTipKey } from '@/lib/helpTips';
import { cn } from '@/lib/utils';

interface HelpTipProps {
  tipKey: HelpTipKey;
  children: React.ReactNode;
  className?: string;
}

const DISPLAY_LABELS: Record<HelpTipKey, string> = {
  evidenceTier:             'Evidence Tier',
  synergy:                  'Synergy',
  redundancy:               'Redundancy',
  interference:             'Interference',
  communitySignal:          'Community Signal',
  reviewRequired:           'Review Required',
  counterfactual:           'Counterfactual',
  pathwayOverlap:           'Pathway Overlap',
  mechanisticEvidence:      'Mechanistic Evidence',
  flaggedInSourceData:      'Flagged in Source Data',
  reportedDrugInteractions: 'Drug Interactions',
};

// Fixed panel width (matches the `w-56` class below) used as a measurement
// fallback in environments without real layout (e.g. jsdom in tests).
const PANEL_WIDTH_FALLBACK = 224;
const PANEL_HEIGHT_FALLBACK = 96;
const VIEWPORT_MARGIN = 8;
const TRIGGER_GAP = 8;

interface PanelPosition {
  top: number;
  left: number;
  maxHeight: number;
  placement: 'top' | 'bottom';
}

export function HelpTip({ tipKey, children, className }: HelpTipProps) {
  const [open, setOpen] = useState(false);
  const [position, setPosition] = useState<PanelPosition | null>(null);
  const triggerRef = useRef<HTMLSpanElement>(null);
  const panelRef = useRef<HTMLDivElement>(null);
  const rawId = useId();
  const panelId = `helptip-${rawId.replace(/:/g, '')}`;

  // Compute a viewport-aware position for the (portalled) panel: prefer
  // opening above the trigger, flip below when there isn't room, and clamp
  // horizontally so the panel never runs off either edge of the screen.
  useLayoutEffect(() => {
    // Nothing to place while closed — the portal itself only renders when
    // `open` is true, so a stale position here never becomes visible; the
    // next open recomputes it synchronously (below) before paint.
    if (!open) return;

    function place() {
      const trigger = triggerRef.current;
      if (!trigger) return;
      const triggerRect = trigger.getBoundingClientRect();
      const panelWidth = panelRef.current?.offsetWidth || PANEL_WIDTH_FALLBACK;
      const panelHeight = panelRef.current?.offsetHeight || PANEL_HEIGHT_FALLBACK;
      const viewportWidth = window.innerWidth;

      const maxLeft = Math.max(viewportWidth - panelWidth - VIEWPORT_MARGIN, VIEWPORT_MARGIN);
      const left = Math.min(Math.max(triggerRect.left, VIEWPORT_MARGIN), maxLeft);

      const spaceAbove = Math.max(0, triggerRect.top - TRIGGER_GAP - VIEWPORT_MARGIN);
      const spaceBelow = Math.max(0, window.innerHeight - triggerRect.bottom - TRIGGER_GAP - VIEWPORT_MARGIN);
      const placement: 'top' | 'bottom' = spaceAbove >= panelHeight || spaceAbove > spaceBelow ? 'top' : 'bottom';
      const maxHeight = Math.max(1, placement === 'top' ? spaceAbove : spaceBelow);
      const visibleHeight = Math.min(panelHeight, maxHeight);
      const top = placement === 'top'
        ? Math.max(VIEWPORT_MARGIN, triggerRect.top - visibleHeight - TRIGGER_GAP)
        : triggerRect.bottom + TRIGGER_GAP;

      setPosition({ top, left, placement, maxHeight });
    }

    place();
    window.addEventListener('resize', place);
    window.addEventListener('scroll', place, true);
    return () => {
      window.removeEventListener('resize', place);
      window.removeEventListener('scroll', place, true);
    };
  }, [open]);

  useEffect(() => {
    if (!open) return;

    function handleKeyDown(e: KeyboardEvent) {
      if (e.key === 'Escape') {
        if (panelRef.current?.contains(document.activeElement)) triggerRef.current?.focus();
        setOpen(false);
      }
    }
    function handleMouseDown(e: MouseEvent) {
      const inTrigger = triggerRef.current?.contains(e.target as Node) ?? false;
      const inPanel   = panelRef.current?.contains(e.target as Node) ?? false;
      if (!inTrigger && !inPanel) setOpen(false);
    }

    document.addEventListener('keydown', handleKeyDown);
    document.addEventListener('mousedown', handleMouseDown);
    return () => {
      document.removeEventListener('keydown', handleKeyDown);
      document.removeEventListener('mousedown', handleMouseDown);
    };
  }, [open]);

  const placement = position?.placement ?? 'top';

  const panel = open && typeof document !== 'undefined'
    ? createPortal(
        <div
          ref={panelRef}
          id={panelId}
          role="tooltip"
          tabIndex={0}
          onClick={e => e.stopPropagation()}
          onKeyDown={e => { if (e.key !== 'Escape') e.stopPropagation(); }}
          data-placement={placement}
          style={{
            position: 'fixed',
            maxHeight: position?.maxHeight,
            maxWidth: 'calc(100vw - 16px)',
            overflowY: 'auto',
            top: position?.top ?? 0,
            left: position?.left ?? 0,
            // Hidden until the first layout pass has a real position, so the
            // panel never flashes at (0, 0) before it is placed.
            visibility: position ? 'visible' : 'hidden',
          }}
          className="z-50 w-56 rounded-lg border border-white/10 bg-[#1e293b] p-3 shadow-[0_8px_24px_rgba(0,0,0,0.4)]"
        >
          <p className="mb-1.5 text-[10px] font-bold uppercase tracking-[0.1em] text-white/40">
            {DISPLAY_LABELS[tipKey]}
          </p>
          <p className="text-xs leading-relaxed text-white/70">
            {helpTips[tipKey]}
          </p>
          {/* CSS caret arrow — points up when the panel sits below the trigger, down otherwise */}
          <div
            aria-hidden="true"
            className={cn(
              'absolute h-2 w-2 rotate-45 bg-[#1e293b] border-white/10',
              placement === 'bottom' ? '-top-[5px] left-3 border-l border-t' : '-bottom-[5px] left-3 border-b border-r'
            )}
          />
        </div>,
        document.body
      )
    : null;

  return (
    <span
        ref={triggerRef}
        role="button"
        tabIndex={0}
        aria-haspopup="true"
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

        {panel}
      </span>
  );
}
