'use client';

import { Shield } from 'lucide-react';
import { cn } from '@/lib/utils';

export interface ReceiptBadgeProps {
  receiptUri: string;       // e.g. "keon://receipt/abc123"
  effectStatus: string;     // "commentary-only" | "non-effecting"
  onClick?: () => void;
  className?: string;
}

function effectStatusDot(effectStatus: string): string {
  switch (effectStatus) {
    case 'non-effecting':
      return 'bg-blue-400';
    case 'commentary-only':
    default:
      return 'bg-white/40';
  }
}

function truncateUri(uri: string, maxLen = 28): string {
  if (uri.length <= maxLen) return uri;
  return uri.slice(0, maxLen) + '…';
}

export function ReceiptBadge({
  receiptUri,
  effectStatus,
  onClick,
  className,
}: ReceiptBadgeProps) {
  return (
    <button
      type="button"
      onClick={onClick}
      disabled={!onClick}
      aria-label={`Receipt: ${receiptUri}`}
      className={cn(
        'inline-flex items-center gap-1.5 rounded-full border border-white/10',
        'bg-white/5 px-2 py-0.5 text-[10px] font-mono text-white/50',
        'transition-colors',
        onClick
          ? 'cursor-pointer hover:border-white/20 hover:bg-white/10 hover:text-white/70'
          : 'cursor-default',
        className,
      )}
    >
      <Shield className="w-2.5 h-2.5 shrink-0 text-white/40" aria-hidden />
      <span>{truncateUri(receiptUri)}</span>
      <span
        className={cn(
          'w-1.5 h-1.5 rounded-full shrink-0',
          effectStatusDot(effectStatus),
        )}
        title={effectStatus}
      />
    </button>
  );
}
