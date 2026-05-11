'use client';

import { useState, useEffect, useRef } from 'react';
import { cn } from '@/lib/utils';
import { track } from '@/lib/telemetry';

interface WhyDrawerProps {
  surface: string;
  metric?: string;
  title?: string;
  inputs: Array<{ label: string; value: string | number | null | undefined }>;
  reasoning: string;
  caveats?: string[];
  children?: React.ReactNode;
  className?: string;
}

export function WhyDrawer({
  surface,
  metric,
  title = 'Why this result?',
  inputs,
  reasoning,
  caveats,
  children,
  className,
}: WhyDrawerProps) {
  const [open, setOpen] = useState(false);
  const dialogRef = useRef<HTMLDialogElement>(null);

  useEffect(() => {
    if (open) {
      track({ name: 'why_drawer_open', surface, metric });
      dialogRef.current?.showModal();
    } else {
      dialogRef.current?.close();
    }
  }, [open, surface, metric]);

  useEffect(() => {
    const el = dialogRef.current;
    if (!el) return;
    const onClose = () => setOpen(false);
    el.addEventListener('close', onClose);
    return () => el.removeEventListener('close', onClose);
  }, []);

  return (
    <span className={cn('inline-flex items-center', className)}>
      <button
        onClick={() => setOpen(true)}
        className="inline-flex items-center gap-1 text-[10px] font-semibold text-white/30 hover:text-white/60 hover:bg-white/5 rounded-full px-2 py-0.5 transition-colors border border-transparent hover:border-white/10"
        aria-label={`Why? — ${surface}`}
      >
        <svg className="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 16 16" strokeWidth="1.5">
          <circle cx="8" cy="8" r="6.5" />
          <path d="M8 11v-1" strokeWidth="2" strokeLinecap="round" />
          <path d="M8 8.5V8a2 2 0 1 0-2-2" strokeLinecap="round" />
        </svg>
        Why?
      </button>

      <dialog
        ref={dialogRef}
        className="fixed inset-0 m-auto w-full max-w-md rounded-3xl border border-white/10 bg-[#0F141B] p-0 shadow-2xl backdrop:bg-black/60 backdrop:backdrop-blur-sm open:flex open:flex-col"
        onClick={(e) => { if (e.target === dialogRef.current) setOpen(false); }}
      >
        {/* Header */}
        <div className="flex items-center justify-between px-6 pt-5 pb-4 border-b border-white/8">
          <div>
            <p className="text-[10px] font-bold text-white/30 uppercase tracking-widest mb-0.5">{surface}</p>
            <h3 className="text-sm font-semibold text-white/90">{title}</h3>
          </div>
          <button
            onClick={() => setOpen(false)}
            className="p-2 rounded-xl hover:bg-white/5 text-white/30 hover:text-white/60 transition-colors"
          >
            <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        <div className="flex-1 overflow-y-auto px-6 py-5 space-y-5">
          {/* Inputs */}
          {inputs.length > 0 && (
            <div>
              <p className="text-[10px] font-bold text-white/30 uppercase tracking-widest mb-2">Inputs Used</p>
              <div className="space-y-1.5">
                {inputs.map(({ label, value }) => (
                  <div key={label} className="flex justify-between items-center gap-4 text-xs">
                    <span className="text-white/50">{label}</span>
                    <span className="font-mono text-white/80 text-right shrink-0">
                      {value === null || value === undefined ? <span className="text-white/25 italic">none</span> : String(value)}
                    </span>
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* Reasoning */}
          <div>
            <p className="text-[10px] font-bold text-white/30 uppercase tracking-widest mb-2">Reasoning</p>
            <p className="text-xs text-white/70 leading-relaxed">{reasoning}</p>
          </div>

          {/* Caveats */}
          {caveats && caveats.length > 0 && (
            <div className="rounded-2xl border border-amber-400/15 bg-amber-500/5 px-4 py-3">
              <p className="text-[10px] font-bold text-amber-400/70 uppercase tracking-widest mb-2">Caveats</p>
              <ul className="space-y-1">
                {caveats.map((c) => (
                  <li key={c} className="text-xs text-amber-300/70 leading-relaxed flex gap-2">
                    <span className="shrink-0 mt-0.5">·</span>
                    {c}
                  </li>
                ))}
              </ul>
            </div>
          )}

          {children}
        </div>

        {/* Footer */}
        <div className="px-6 py-4 border-t border-white/5">
          <p className="text-[10px] text-white/25 leading-relaxed">
            This explanation reflects the system's inputs and logic at the time of calculation. It is observational and does not constitute medical advice.
          </p>
        </div>
      </dialog>
    </span>
  );
}
