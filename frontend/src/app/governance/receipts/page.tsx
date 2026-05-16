'use client';

import { useEffect, useState } from 'react';
import { useSearchParams } from 'next/navigation';
import { Shield, Loader2, AlertCircle } from 'lucide-react';
import { cn } from '@/lib/utils';
import { apiClient } from '@/lib/api';
import { isEnabled } from '@/lib/flags';
import { ReceiptDrawer } from '@/components/governance/ReceiptDrawer';
import { Header } from '@/components/Header';
import type { DecisionReceiptResponse } from '@/lib/types';

type SubjectFilter =
  | 'all'
  | 'protocol'
  | 'compound'
  | 'review'
  | 'export'
  | 'analyzer-conversion'
  | 'promotion';

const FILTER_PILLS: { id: SubjectFilter; label: string }[] = [
  { id: 'all', label: 'All' },
  { id: 'protocol', label: 'Protocol' },
  { id: 'compound', label: 'Compound' },
  { id: 'review', label: 'Review' },
  { id: 'export', label: 'Export' },
  { id: 'analyzer-conversion', label: 'Analyzer' },
  { id: 'promotion', label: 'Promotion' },
];

function actionLabel(subjectUri: string): string {
  const prefix = subjectUri.split(':')[0] ?? '';
  const labels: Record<string, string> = {
    protocol: 'Protocol Review',
    compound: 'Compound Classification',
    review: 'Stack Review',
    export: 'Data Export',
    'analyzer-conversion': 'Analyzer Conversion',
    promotion: 'Promotion Gate',
  };
  return labels[prefix] ?? 'Governed Effect';
}

function DecisionPill({ decision }: { decision: string }) {
  const styles: Record<string, string> = {
    allowed: 'border-emerald-500/30 bg-emerald-500/10 text-emerald-400',
    blocked: 'border-red-500/30 bg-red-500/10 text-red-400',
    escalate: 'border-amber-500/30 bg-amber-500/10 text-amber-400',
  };
  const cls = styles[decision.toLowerCase()] ?? 'border-white/10 bg-white/5 text-white/50';
  return (
    <span
      className={cn(
        'inline-flex items-center rounded-full border px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wider',
        cls,
      )}
    >
      {decision}
    </span>
  );
}

function EffectPill({ status }: { status: string }) {
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

function formatTimestamp(iso: string): string {
  try {
    return new Date(iso).toLocaleString(undefined, {
      dateStyle: 'medium',
      timeStyle: 'short',
    });
  } catch {
    return iso;
  }
}

export default function AuditReceiptFeedPage() {
  if (!isEnabled('decisionTheater')) {
    return null;
  }

  return <AuditReceiptFeedContent />;
}

function AuditReceiptFeedContent() {
  const searchParams = useSearchParams();
  const subjectParam = searchParams.get('subject');

  const [receipts, setReceipts] = useState<DecisionReceiptResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [activeFilter, setActiveFilter] = useState<SubjectFilter>('all');
  const [drawerUri, setDrawerUri] = useState<string>('');
  const [drawerOpen, setDrawerOpen] = useState(false);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setError(null);

    const fetch = subjectParam
      ? apiClient.getReceiptsBySubject(subjectParam)
      : apiClient.getReceiptsByActor('biostack-system');

    fetch
      .then((data) => {
        if (!cancelled) setReceipts(data);
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : 'Failed to load receipts');
        }
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });

    return () => { cancelled = true; };
  }, [subjectParam]);

  const filtered =
    activeFilter === 'all'
      ? receipts
      : receipts.filter((r) => r.subjectUri.startsWith(`${activeFilter}:`));

  function openDrawer(uri: string) {
    setDrawerUri(uri);
    setDrawerOpen(true);
  }

  return (
    <div className="w-full">
      <Header
        title="Audit Receipt Feed"
        subtitle="Immutable log of governed effects"
      />

      <div className="max-w-4xl p-6 space-y-5">
        {/* Filter pills */}
        <div
          role="group"
          aria-label="Filter by subject type"
          className="flex flex-wrap gap-2"
        >
          {FILTER_PILLS.map((pill) => (
            <button
              key={pill.id}
              type="button"
              onClick={() => setActiveFilter(pill.id)}
              aria-pressed={activeFilter === pill.id}
              className={cn(
                'rounded-full border px-3 py-1 text-xs font-medium transition-colors',
                activeFilter === pill.id
                  ? 'border-emerald-500/40 bg-emerald-500/10 text-emerald-300'
                  : 'border-white/10 bg-white/[0.03] text-white/40 hover:border-white/20 hover:text-white/60',
              )}
            >
              {pill.label}
            </button>
          ))}
        </div>

        {/* Body */}
        {loading && (
          <div className="flex items-center justify-center gap-2 py-16 text-white/40 text-sm">
            <Loader2 className="w-4 h-4 animate-spin" aria-hidden />
            Loading receipts...
          </div>
        )}

        {!loading && error && (
          <div className="flex items-start gap-2 rounded-lg border border-red-500/20 bg-red-500/10 p-4 text-red-400 text-sm">
            <AlertCircle className="w-4 h-4 mt-0.5 shrink-0" aria-hidden />
            {error}
          </div>
        )}

        {!loading && !error && filtered.length === 0 && (
          <div className="flex flex-col items-center justify-center gap-3 py-20 text-center">
            <Shield className="w-8 h-8 text-white/20" aria-hidden />
            <p className="text-sm text-white/40">
              No governed actions yet — receipts appear here when governed effects are produced.
            </p>
          </div>
        )}

        {!loading && !error && filtered.length > 0 && (
          <ul role="list" className="space-y-2">
            {filtered.map((receipt) => (
              <li key={receipt.receiptUri}>
                <button
                  type="button"
                  onClick={() => openDrawer(receipt.receiptUri)}
                  className={cn(
                    'w-full text-left rounded-lg border border-white/[0.07] bg-white/[0.02]',
                    'px-4 py-3 transition-colors',
                    'hover:border-white/[0.14] hover:bg-white/[0.04]',
                    'focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-emerald-500/50',
                  )}
                  aria-label={`View receipt for ${actionLabel(receipt.subjectUri)}`}
                >
                  <div className="flex flex-wrap items-center justify-between gap-3">
                    {/* Left: action label + subject URI */}
                    <div className="min-w-0 space-y-0.5">
                      <p className="text-sm font-medium text-white/80 truncate">
                        {actionLabel(receipt.subjectUri)}
                      </p>
                      <p className="text-[10px] font-mono text-white/30 truncate">
                        {receipt.subjectUri}
                      </p>
                    </div>

                    {/* Right: badges + meta */}
                    <div className="flex flex-wrap items-center gap-2 shrink-0">
                      <DecisionPill decision={receipt.decision} />
                      <EffectPill status={receipt.effectStatus} />
                      <span className="font-mono text-[10px] text-white/30">
                        v{receipt.policyHash.version}
                      </span>
                      <span className="text-[10px] text-white/25">
                        {formatTimestamp(receipt.timestampUtc)}
                      </span>
                    </div>
                  </div>
                </button>
              </li>
            ))}
          </ul>
        )}
      </div>

      <ReceiptDrawer
        receiptUri={drawerUri}
        open={drawerOpen}
        onClose={() => setDrawerOpen(false)}
      />
    </div>
  );
}
