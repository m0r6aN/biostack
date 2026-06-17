'use client';

import { useEffect } from 'react';
import { X } from 'lucide-react';

interface ModalShellProps {
  onClose: () => void;
  /** id of the element labelling the dialog (for aria-labelledby). */
  labelledBy?: string;
  children: React.ReactNode;
  className?: string;
}

/**
 * Shared modal chrome for the protocol portal: fixed backdrop, centered panel,
 * Esc-to-close, backdrop-click-to-close, and the `modal-pop` entry animation
 * (reduced-motion aware via globals.css).
 */
export function ModalShell({ onClose, labelledBy, children, className }: ModalShellProps) {
  useEffect(() => {
    function onKey(event: KeyboardEvent) {
      if (event.key === 'Escape') onClose();
    }
    document.addEventListener('keydown', onKey);
    return () => document.removeEventListener('keydown', onKey);
  }, [onClose]);

  return (
    <div
      className="fixed inset-0 z-[100] flex items-center justify-center bg-black/60 p-4 backdrop-blur-sm"
      onClick={onClose}
    >
      <div
        role="dialog"
        aria-modal="true"
        aria-labelledby={labelledBy}
        onClick={(event) => event.stopPropagation()}
        className={`modal-pop relative w-full max-w-lg rounded-2xl border border-white/10 bg-[#121923] p-6 shadow-[0_8px_40px_rgba(0,0,0,0.6)] ${className ?? ''}`}
      >
        <button
          type="button"
          onClick={onClose}
          aria-label="Close"
          className="absolute right-4 top-4 rounded-lg p-1 text-white/40 transition-colors hover:bg-white/5 hover:text-white/80"
        >
          <X className="h-5 w-5" />
        </button>
        {children}
      </div>
    </div>
  );
}
