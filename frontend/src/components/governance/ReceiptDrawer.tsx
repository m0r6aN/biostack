'use client';

import { useEffect, useState } from 'react';
import { X, Shield, AlertCircle, Loader2 } from 'lucide-react';
import { cn } from '@/lib/utils';
import { apiClient } from '@/lib/api';
import type { DecisionReceiptResponse } from '@/lib/types';

export interface ReceiptDrawerProps {
  receiptUri: string;
  open: boolean;
  onClose: () => void;
}

function FieldRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="grid grid-cols-[140px_1fr] gap-2 py-2 border-b border-white/5 last:border-0">
      <span className="text-[10px] font-bold uppercase tracking-widest text-white/30 pt-0.5">
        {label}
      </span>
      <span className="text-xs font-mono text-white/70 break-all">{value}</span>
    </div>
  );
}

function EffectStatusPill({ status }: { status: string }) {
  const isNonEffecting = status === 'non-effecting';
  return (
    <span
      className={cn(
        'inline-flex items-center gap-1 rounded-full border px-2 py-0.5 text-[10px] font-medium',
        isNonEffecting
          ? 'border-blue-500/30 bg-blue-500/10 text-blue-400'
          : 'border-white/10 bg-white/5 text-white/50',
      )}
    >
      <span
        className={cn(
          'w-1.5 h-1.5 rounded-full shrink-0',
          isNonEffecting ? 'bg-blue-400' : 'bg-white/40',
        )}
      />
      {status}
    </span>
  );
}

export function ReceiptDrawer({ receiptUri, open, onClose }: ReceiptDrawerProps) {
  const [receipt, setReceipt] = useState<DecisionReceiptResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!open || !receiptUri) return;

    let cancelled = false;
    setLoading(true);
    setError(null);
    setReceipt(null);

    apiClient
      .getReceipt(receiptUri)
      .then((data: DecisionReceiptResponse) => {
        if (!cancelled) setReceipt(data);
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          const message = err instanceof Error ? err.message : 'Failed to load receipt';
          setError(message);
        }
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });

    return () => { cancelled = true; };
  }, [open, receiptUri]);

  // Close on Escape
  useEffect(() => {
    if (!open) return;
    const handler = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose();
    };
    window.addEventListener('keydown', handler);
    return () => window.removeEventListener('keydown', handler);
  }, [open, onClose]);

  if (!open) return null;

  return (
    <>
      {/* Backdrop */}
      <div
        className="fixed inset-0 z-40 bg-black/60 backdrop-blur-sm"
        onClick={onClose}
        aria-hidden
      />

      {/* Panel */}
      <aside
        role="dialog"
        aria-modal="true"
        aria-label="Decision Receipt"
        className={cn(
          'fixed right-0 top-0 z-50 h-full w-full max-w-md',
          'bg-[#0d0d0f] border-l border-white/10 flex flex-col',
          'shadow-2xl',
        )}
      >
        {/* Header */}
        <div className="flex items-center justify-between px-5 py-4 border-b border-white/10">
          <div className="flex items-center gap-2">
            <Shield className="w-4 h-4 text-white/40" aria-hidden />
            <span className="text-xs font-bold uppercase tracking-widest text-white/60">
              Decision Receipt
            </span>
          </div>
          <button
            type="button"
            onClick={onClose}
            aria-label="Close receipt drawer"
            className="rounded p-1 text-white/30 hover:text-white/70 hover:bg-white/5 transition-colors"
          >
            <X className="w-4 h-4" />
          </button>
        </div>

        {/* Body */}
        <div className="flex-1 overflow-y-auto px-5 py-4">
          {loading && (
            <div className="flex items-center gap-2 text-white/40 text-sm py-8 justify-center">
              <Loader2 className="w-4 h-4 animate-spin" />
              Loading receipt...
            </div>
          )}

          {error && (
            <div className="flex items-start gap-2 rounded-lg border border-red-500/20 bg-red-500/10 p-4 text-red-400 text-sm">
              <AlertCircle className="w-4 h-4 mt-0.5 shrink-0" />
              {error}
            </div>
          )}

          {receipt && (
            <div className="space-y-4">
              {/* Status pill + URI */}
              <div className="space-y-2">
                <EffectStatusPill status={receipt.effectStatus} />
                <p className="text-[10px] font-mono text-white/30 break-all">
                  {receipt.receiptUri}
                </p>
              </div>

              {/* Fields */}
              <div className="rounded-lg border border-white/5 bg-white/[0.02] px-4 py-1">
                <FieldRow label="Subject" value={receipt.subjectUri} />
                <FieldRow label="Tenant" value={receipt.tenantId} />
                <FieldRow label="Actor" value={receipt.actorId} />
                <FieldRow
                  label="Timestamp"
                  value={new Date(receipt.timestampUtc).toISOString()}
                />
                <FieldRow label="Decision" value={receipt.decision} />
                <FieldRow label="Effect" value={receipt.effectStatus} />
                <FieldRow label="Input hash" value={receipt.inputHash} />
                <FieldRow label="Policy hash" value={receipt.policyHash.value} />
                <FieldRow label="Policy ver." value={receipt.policyHash.version} />
              </div>

              {/* Evidence refs */}
              <div>
                <p className="text-[9px] font-bold uppercase tracking-widest text-white/20 mb-2">
                  Evidence References
                </p>
                {receipt.evidenceRefs.length === 0 ? (
                  <p className="text-xs text-white/30 italic">None</p>
                ) : (
                  <ul className="space-y-1">
                    {receipt.evidenceRefs.map((ref: string, i: number) => (
                      <li
                        key={i}
                        className="text-xs font-mono text-white/50 bg-white/5 rounded px-2 py-1 break-all"
                      >
                        {ref}
                      </li>
                    ))}
                  </ul>
                )}
              </div>
            </div>
          )}
        </div>
      </aside>
    </>
  );
}
