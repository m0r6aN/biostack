'use client';

import Link from 'next/link';
import { useEffect, useState } from 'react';
import { useOptionalAuth } from '@/lib/AuthProvider';
import { formatAsOf } from '@/lib/claimVerdicts';
import { watchlistApi, watchTargetPath, type AlertFrequency, type AlertNotice, type AlertPreferences, type Watch } from '@/lib/watchlist';

const FREQUENCY_COPY: Record<AlertFrequency, string> = {
  instant: 'Verdict changes right away (at most one email per compound a day); new studies weekly',
  weekly: 'Everything in one weekly email',
  off: 'No emails: in-app notices only',
};

export function noticeLine(n: AlertNotice): string {
  return n.alertType === 'verdict-change'
    ? `Verdict moved: ${n.oldVerdict} → ${n.newVerdict}. ${n.reason}`
    : `New evidence added. Verdict unchanged (${n.newVerdict}).`;
}

export function AlertSettings() {
  const auth = useOptionalAuth();
  const [prefs, setPrefs] = useState<AlertPreferences | null>(null);
  const [watches, setWatches] = useState<Watch[]>([]);
  const [notices, setNotices] = useState<AlertNotice[]>([]);
  const [saved, setSaved] = useState(false);
  const signedIn = Boolean(auth?.user);

  useEffect(() => {
    if (!signedIn) return;
    watchlistApi.preferences().then(setPrefs).catch(() => {});
    watchlistApi.list().then(setWatches).catch(() => {});
    watchlistApi.notices().then(setNotices).catch(() => {});
  }, [signedIn]);

  if (!auth?.loading && !signedIn) {
    return <p className="text-white/70"><Link href="/auth/signin?callbackUrl=/account/alerts" className="text-cyan-300 underline">Sign in</Link> to manage alerts.</p>;
  }

  const save = async (next: AlertPreferences) => {
    setPrefs(next); setSaved(false);
    try { setPrefs(await watchlistApi.savePreferences(next)); setSaved(true); } catch { /* keep optimistic value */ }
  };

  return (
    <div className="space-y-8">
      <section aria-label="Email frequency" className="space-y-2">
        <h2 className="text-lg font-medium text-white">How often</h2>
        {prefs && (Object.keys(FREQUENCY_COPY) as AlertFrequency[]).map(f => (
          <label key={f} className="flex items-start gap-2 text-sm text-white/80">
            <input type="radio" name="frequency" value={f} checked={prefs.frequency === f} onChange={() => save({ ...prefs, frequency: f })} />
            {FREQUENCY_COPY[f]}
          </label>
        ))}
        {prefs && (
          <label className="flex items-center gap-2 text-sm text-white/80">
            <input type="checkbox" checked={prefs.emailEnabled} onChange={e => save({ ...prefs, emailEnabled: e.target.checked })} />
            Send alert emails
          </label>
        )}
        {saved && <p className="text-xs text-emerald-300" role="status">Saved.</p>}
      </section>

      <section aria-label="Watching" className="space-y-2">
        <h2 className="text-lg font-medium text-white">Watching</h2>
        {watches.length === 0 ? <p className="text-sm text-white/60">Nothing watched yet. Watch a compound and we&rsquo;ll tell you when the evidence moves, not when the hype does.</p> : (
          <ul className="space-y-1">
            {watches.map(w => (
              <li key={w.id} className="flex items-center justify-between text-sm">
                <Link href={watchTargetPath(w.targetType, w.targetId)} className="text-cyan-300 underline underline-offset-4">{w.targetId}</Link>
                <button type="button" className="text-white/60 hover:text-white"
                  onClick={() => watchlistApi.remove(w.id).then(() => setWatches(ws => ws.filter(x => x.id !== w.id)), () => {})}>
                  Unwatch
                </button>
              </li>
            ))}
          </ul>
        )}
      </section>

      <section aria-label="Notices" className="space-y-2">
        <h2 className="text-lg font-medium text-white">Recent notices</h2>
        {notices.length === 0 ? <p className="text-sm text-white/60">No changes on anything you watch yet.</p> : (
          <ul className="space-y-2">
            {notices.map(n => (
              <li key={n.id} className="text-sm text-white/80">
                <span className="text-white/50">{formatAsOf(n.createdAtUtc)} · </span>
                <Link href={`/knowledge/${n.compoundSlug}/changelog`} className="text-cyan-300 underline underline-offset-4">{n.compoundSlug}</Link>: {noticeLine(n)}
              </li>
            ))}
          </ul>
        )}
      </section>
    </div>
  );
}
