'use client';

import Link from 'next/link';
import { ProtocolRun } from '@/lib/types';

interface ActiveProtocolRunCardProps {
  run: ProtocolRun | null;
}

export function ActiveProtocolRunCard({ run }: ActiveProtocolRunCardProps) {
  if (!run) {
    return null;
  }

  return (
    <Link
      href={`/protocols/${run.protocolId}`}
      className="block rounded-lg border border-emerald-400/20 bg-emerald-500/[0.06] p-5 transition-colors hover:border-emerald-300/40"
    >
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <p className="text-xs font-semibold uppercase tracking-[0.16em] text-emerald-200/55">Active Protocol</p>
          <h2 className="mt-2 text-xl font-bold text-white">Currently Running: {run.protocolName} v{run.protocolVersion}</h2>
          <p className="mt-1 text-sm text-white/45">Started {new Date(run.startedAtUtc).toLocaleDateString()}</p>
        </div>
        <span className="rounded-lg border border-emerald-400/25 bg-emerald-500/10 px-3 py-1.5 text-sm font-semibold text-emerald-100">
          View run
        </span>
      </div>
    </Link>
  );
}
