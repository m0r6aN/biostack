'use client';

import { useState } from 'react';
import type { ProtocolAnalyzerResult } from '@/lib/types';

// ── ParsedProtocolSection ─────────────────────────────────────────────────────
// Moved from monolith ~1032-1107, verbatim table + mobile cards + blend badge.
// Added collapsible body: collapsed by default only when protocol.length > 6.

export interface ParsedProtocolSectionProps {
  result: ProtocolAnalyzerResult | null;
}

export function ParsedProtocolSection({ result }: ParsedProtocolSectionProps) {
  // Collapsed by default ONLY when there are more than 6 items; expanded otherwise.
  const [collapsed, setCollapsed] = useState(() =>
    result != null && result.protocol.length > 6,
  );

  const itemCount = result?.protocol.length ?? 0;

  return (
    <section className="rounded-lg border border-white/[0.08] bg-white/[0.03] p-4">
      <div className="flex items-center justify-between gap-3">
        <button
          type="button"
          onClick={() => setCollapsed((c) => !c)}
          className="flex items-center gap-2 text-lg font-semibold text-white transition-colors hover:text-white/80"
        >
          <span>Parsed Protocol</span>
          {itemCount > 0 && (
            <span className="rounded-full border border-white/10 px-2 py-0.5 text-xs font-semibold text-white/55">
              {itemCount} {itemCount === 1 ? 'item' : 'items'}
            </span>
          )}
          <span className="ml-1 text-white/40">{collapsed ? '▸' : '▾'}</span>
        </button>
        {result?.decomposedBlends.length ? (
          <span className="rounded-lg border border-emerald-300/20 bg-emerald-400/10 px-3 py-1 text-xs font-semibold uppercase tracking-[0.14em] text-emerald-100">
            Blend detected
          </span>
        ) : null}
      </div>

      {!collapsed && (
        <>
          {result?.decomposedBlends.length ? (
            <div className="mt-4 rounded-lg border border-white/10 bg-black/20 p-3 text-sm text-white/58">
              {result.decomposedBlends.map((blend) => (
                <p key={blend.blendName}>
                  {blend.blendName}: {blend.components.join(', ')}
                </p>
              ))}
            </div>
          ) : null}

          <div className="mt-4 hidden overflow-x-auto md:block">
            <table className="w-full min-w-[560px] text-left text-sm">
              <thead className="border-b border-white/10 text-xs uppercase tracking-[0.16em] text-white/35">
                <tr>
                  <th className="py-3 font-semibold">Compound</th>
                  <th className="py-3 font-semibold">Dose</th>
                  <th className="py-3 font-semibold">Frequency</th>
                  <th className="py-3 font-semibold">Duration</th>
                  <th className="py-3 font-semibold">Status</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-white/[0.06] text-white/72">
                {result && result.protocol.length > 0 ? (
                  result.protocol.map((entry) => (
                    <tr key={`${entry.compoundName}-${entry.dose}-${entry.frequency}`}>
                      <td className="py-3 font-semibold text-white">{entry.compoundName}</td>
                      <td className="py-3">{entry.dose > 0 ? `${entry.dose} ${entry.unit}` : 'Unknown'}</td>
                      <td className="py-3">{entry.frequency || 'Unknown'}</td>
                      <td className="py-3">{entry.duration || 'Unspecified'}</td>
                      <td className="py-3">
                        {entry.dose > 0 ? (
                          <span className="rounded-lg border border-emerald-300/20 bg-emerald-400/10 px-2 py-1 text-xs font-semibold text-emerald-100">Canonicalized</span>
                        ) : (
                          <span className="rounded-lg border border-amber-300/20 bg-amber-400/10 px-2 py-1 text-xs font-semibold text-amber-100">Partial</span>
                        )}
                      </td>
                    </tr>
                  ))
                ) : (
                  <tr>
                    <td className="py-5 text-white/45" colSpan={5}>No parsed compounds yet.</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>

          <div className="mt-4 grid gap-3 md:hidden">
            {result?.protocol.length ? (
              result.protocol.map((entry) => (
                <article key={`${entry.compoundName}-${entry.dose}-${entry.frequency}`} className="rounded-lg border border-white/10 bg-black/20 p-3">
                  <p className="font-semibold text-white">{entry.compoundName}</p>
                  <p className="mt-2 text-sm text-white/65">Dose: {entry.dose > 0 ? `${entry.dose} ${entry.unit}` : 'Unknown'}</p>
                  <p className="text-sm text-white/65">Frequency: {entry.frequency || 'Unknown'}</p>
                  <p className="text-sm text-white/65">Duration: {entry.duration || 'Unspecified'}</p>
                </article>
              ))
            ) : (
              <p className="text-sm text-white/45">No parsed compounds yet.</p>
            )}
          </div>
        </>
      )}
    </section>
  );
}
