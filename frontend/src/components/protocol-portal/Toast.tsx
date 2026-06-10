'use client';

import { useEffect } from 'react';
import { CheckCircle2 } from 'lucide-react';

interface ToastProps {
  message: string;
  onDismiss: () => void;
  /** Auto-dismiss delay in ms. */
  duration?: number;
}

/**
 * Minimal transient toast. The app has no toast primitive yet; this is
 * self-contained and reduced-motion aware (animation is opt-in via `.modal-pop`,
 * which is disabled under prefers-reduced-motion in globals.css).
 */
export function Toast({ message, onDismiss, duration = 2800 }: ToastProps) {
  useEffect(() => {
    const timer = setTimeout(onDismiss, duration);
    return () => clearTimeout(timer);
  }, [onDismiss, duration]);

  return (
    <div
      role="status"
      aria-live="polite"
      className="modal-pop fixed bottom-6 left-1/2 z-[200] flex -translate-x-1/2 items-center gap-2 rounded-2xl border border-white/10 bg-[#121923] px-6 py-3 text-sm text-white shadow-[0_8px_40px_rgba(0,0,0,0.6)]"
    >
      <CheckCircle2 className="h-4 w-4 text-emerald-400" />
      <span>{message}</span>
    </div>
  );
}
