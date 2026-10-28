'use client';

import { DoseLogForm } from '@/components/protocol-portal/DoseLogForm';
import { apiClient } from '@/lib/api';
import type { CompoundRecord, ScheduleItem } from '@/lib/types';
import { formatDoseAmount } from '@/lib/userDose';
import { describeFrequency } from '@/lib/userFrequency';
import Link from 'next/link';
import { useEffect, useState } from 'react';

type LogEntry = { amount: number; unit: string; note: string };

/** A view of the user's own records. Legacy records have no confirmed catalog link. */
export function TakingTab({ profileId, onLogAmount }: {
  profileId: string;
  onLogAmount: (item: ScheduleItem, entry: LogEntry) => Promise<void>;
}) {
  const [items, setItems] = useState<CompoundRecord[] | null>(null);
  const [error, setError] = useState(false);
  const [attempt, setAttempt] = useState(0);

  useEffect(() => {
    let current = true;
    apiClient.getCompounds(profileId).then(
      (records) => { if (current) { setItems(records); setError(false); } },
      () => { if (current) { setItems(null); setError(true); } },
    );
    return () => { current = false; };
  }, [profileId, attempt]);

  const visible = (items ?? []).filter((item) => ['active', 'paused'].includes(item.status.toLowerCase()));

  return (
    <section className="space-y-4" aria-label="Everything I'm Taking">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h2 className="text-xl font-semibold text-white">Everything I&apos;m Taking</h2>
          <p className="text-sm text-white/60">Only what you entered. BioStack doesn&apos;t suggest amounts.</p>
        </div>
        <Link href="/compounds" className="rounded-lg border border-white/20 px-3 py-2 text-sm text-white">Add or manage items</Link>
      </div>
      {error ? (
        <div role="alert" className="text-sm text-white/70">
          Items could not be loaded.{' '}
          <button type="button" className="underline" onClick={() => { setError(false); setAttempt((n) => n + 1); }}>Try again</button>
        </div>
      ) : items === null ? (
        <p className="text-sm text-white/60">Loading your items…</p>
      ) : visible.length === 0 ? (
        <p className="text-sm text-white/60">No active or paused items yet.</p>
      ) : (
        <div className="space-y-3">
          {visible.map((item) => {
            const scheduleItem: ScheduleItem = {
              name: item.name, time: '', detail: item.category,
              compoundId: item.id, doseAmount: item.doseAmount, doseUnit: item.doseUnit,
            };
            const times = item.frequencyTimesOfDay?.join(', ') || item.doseTime;
            return (
              <article key={item.id} className="rounded-xl border border-white/10 p-4">
                <div className="flex flex-wrap items-center gap-2">
                  <h3 className="font-semibold text-white">{item.name}</h3>
                  <span className="text-xs text-white/60">{item.status}</span>
                  <span title="This entry is not linked to BioStack's catalog." className="text-xs text-white/60">Unverified</span>
                </div>
                <p className="mt-1 text-sm text-white/60">{item.category}</p>
                <p className="mt-1 text-sm text-white/75">
                  {[formatDoseAmount(item), item.doseFrequency, describeFrequency(item), times].filter(Boolean).join(' · ')}
                </p>
                {item.status.toLowerCase() === 'active' && (
                  <DoseLogForm item={scheduleItem} onLog={(entry) => onLogAmount(scheduleItem, entry)} />
                )}
              </article>
            );
          })}
        </div>
      )}
    </section>
  );
}
