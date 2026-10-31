'use client';

import { Header } from '@/components/Header';
import { getApiBaseUrl } from '@/lib/apiBase';
import { useCallback, useEffect, useState } from 'react';

const API_URL = getApiBaseUrl();

type SummaryTable = { name: string; count: number; onDeletion: 'removed' | 'kept'; note: string };
type Summary = {
  tables: SummaryTable[];
  backupRetentionDays: number;
  confirmationPhrase: string;
  deletionAvailable: boolean;
};

const TABLE_LABELS: Record<string, string> = {
  PersonProfiles: 'Profiles',
  CompoundRecords: 'Items you track (Everything I’m Taking)',
  DoseLogEntries: 'Dose logs',
  CheckIns: 'Check-ins',
  Protocols: 'Protocols',
  ProtocolItems: 'Protocol items',
  ProtocolRuns: 'Protocol runs',
  ProtocolPhases: 'Protocol phases',
  ProtocolComputationRecords: 'Saved computations',
  ProtocolReviewCompletedEvents: 'Protocol reviews',
  TimelineEvents: 'Timeline entries',
  ProfileGoals: 'Goals',
  EvidenceWatches: 'Watched compounds',
  EvidenceAlertPreferences: 'Alert settings',
  EvidenceAlertLog: 'Alerts sent to you',
  Subscriptions: 'Subscription records',
  ClaimSubmissions: 'Claims you submitted',
  LeadCaptures: 'Email sign-ups',
  ProviderAccessRequests: 'Provider-access requests (made with your email)',
  ProviderAccessAuditEntries: 'Operator audit entries',
  AuthIdentities: 'Sign-in email',
  AuthChallenges: 'Sign-in and confirmation links issued',
  PasskeyCredentials: 'Passkeys',
  PasskeyOperationChallenges: 'Passkey set-up attempts',
  Sessions: 'Signed-in sessions',
  SpineEntries: 'Governance receipts',
  AppUsers: 'Your account',
};

const label = (name: string) => TABLE_LABELS[name] ?? name;

export default function AccountDataPage() {
  const [summary, setSummary] = useState<Summary | null>(null);
  const [loadState, setLoadState] = useState<'loading' | 'ready' | 'error'>('loading');
  const [exportMessage, setExportMessage] = useState('');
  const [exporting, setExporting] = useState(false);
  const [phrase, setPhrase] = useState('');
  const [deleteMessage, setDeleteMessage] = useState('');
  const [deleteSent, setDeleteSent] = useState(false);
  const [requesting, setRequesting] = useState(false);

  const load = useCallback(async () => {
    setLoadState('loading');
    try {
      const response = await fetch(`${API_URL}/api/v1/account/data-summary`, { credentials: 'include', cache: 'no-store' });
      if (!response.ok) throw new Error('summary-unavailable');
      setSummary(await response.json() as Summary);
      setLoadState('ready');
    } catch {
      setLoadState('error');
    }
  }, []);

  useEffect(() => {
    // The request synchronizes this client surface with server-held account data.
    // eslint-disable-next-line react-hooks/set-state-in-effect
    void load();
  }, [load]);

  async function download(format: 'json' | 'csv') {
    setExporting(true);
    setExportMessage('');
    try {
      const response = await fetch(`${API_URL}/api/v1/account/export?format=${format}`, { credentials: 'include', cache: 'no-store' });
      if (response.status === 429) {
        setExportMessage('You’ve downloaded a few copies already. Wait about ten minutes and try again.');
        return;
      }
      if (!response.ok) throw new Error('export-failed');
      const blob = await response.blob();
      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = format === 'csv' ? 'biostack-export-csv.zip' : 'biostack-export.json';
      document.body.appendChild(link);
      link.click();
      link.remove();
      URL.revokeObjectURL(url);
      setExportMessage('Your download has started.');
    } catch {
      setExportMessage('We couldn’t prepare your export. Check your connection and try again.');
    } finally {
      setExporting(false);
    }
  }

  async function requestDeletion() {
    setRequesting(true);
    setDeleteMessage('');
    try {
      const response = await fetch(`${API_URL}/api/v1/account/deletion/request`, {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ confirmation: phrase }),
      });
      if (response.status === 202) {
        setDeleteSent(true);
        return;
      }
      if (response.status === 429) {
        setDeleteMessage('We’ve already emailed you a few links. Check your inbox, or try again in an hour.');
        return;
      }
      const body = await response.json().catch(() => ({})) as { message?: string };
      setDeleteMessage(body.message ?? 'We couldn’t send the link. Try again.');
    } catch {
      setDeleteMessage('We couldn’t send the link. Check your connection and try again.');
    } finally {
      setRequesting(false);
    }
  }

  const days = summary?.backupRetentionDays ?? 35;
  const required = summary?.confirmationPhrase ?? 'DELETE MY ACCOUNT';
  const removable = summary?.tables.filter(table => table.onDeletion === 'removed' && table.count > 0) ?? [];
  const kept = summary?.tables.filter(table => table.onDeletion === 'kept' && table.count > 0) ?? [];

  return (
    <div className="min-h-full">
      <Header title="Your data" subtitle="Download or delete what BioStack holds about you" />
      <div className="mx-auto max-w-3xl space-y-6 p-5 sm:p-8">
        <section className="rounded-2xl border border-white/[0.07] bg-white/[0.025] p-6">
          <h2 className="text-lg font-semibold text-white">What BioStack holds</h2>
          {loadState === 'loading' && <p className="mt-3 text-sm text-white/60" role="status">Loading your data summary…</p>}
          {loadState === 'error' && (
            <div className="mt-3">
              <p className="text-sm text-amber-100/80">We couldn’t load the summary. Your export and deletion still work.</p>
              <button type="button" onClick={() => void load()} className="mt-3 min-h-11 rounded-lg border border-white/20 px-4 text-sm text-white hover:bg-white/10">
                Reload summary
              </button>
            </div>
          )}
          {loadState === 'ready' && summary && (
            <ul className="mt-4 divide-y divide-white/[0.06] text-sm">
              {summary.tables.filter(table => table.count > 0).map(table => (
                <li key={table.name} className="flex items-center justify-between gap-4 py-2.5">
                  <span className="text-white/75">{label(table.name)}</span>
                  <span className="text-white/50">{table.count}</span>
                </li>
              ))}
            </ul>
          )}
        </section>

        <section className="rounded-2xl border border-white/[0.07] bg-white/[0.025] p-6" aria-labelledby="export-heading">
          <h2 id="export-heading" className="text-lg font-semibold text-white">Download your data</h2>
          <p className="mt-2 text-sm leading-6 text-white/50">
            Get a copy of everything above in one file. JSON keeps the structure; CSV gives you one spreadsheet per list, in a zip. Passwords, link secrets, and passkey keys are never included.
          </p>
          <div className="mt-4 flex flex-col gap-3 sm:flex-row">
            <button type="button" disabled={exporting} onClick={() => void download('json')}
              className="min-h-11 rounded-xl bg-emerald-400 px-5 text-sm font-bold text-[#07110c] hover:bg-emerald-300 disabled:opacity-55">
              {exporting ? 'Preparing…' : 'Download JSON'}
            </button>
            <button type="button" disabled={exporting} onClick={() => void download('csv')}
              className="min-h-11 rounded-xl border border-white/20 px-5 text-sm font-semibold text-white hover:bg-white/10 disabled:opacity-55">
              Download CSV (zip)
            </button>
          </div>
          {exportMessage && <p className="mt-4 text-sm text-white/60" role="status">{exportMessage}</p>}
        </section>

        <section className="rounded-2xl border border-red-300/15 bg-red-400/[0.04] p-6" aria-labelledby="delete-heading">
          <h2 id="delete-heading" className="text-lg font-semibold text-white">Delete your account</h2>
          {summary && !summary.deletionAvailable ? (
            <p className="mt-2 text-sm leading-6 text-white/55">
              Operator accounts can’t be deleted from here. Contact BioStack to retire an operator account.
            </p>
          ) : deleteSent ? (
            <p className="mt-3 rounded-xl border border-emerald-300/20 bg-emerald-400/[0.06] p-4 text-sm leading-6 text-emerald-50/85" role="status">
              We emailed you a link. Open it in this browser within 15 minutes to finish. Nothing has been deleted yet.
            </p>
          ) : (
            <>
              <p className="mt-2 text-sm leading-6 text-white/55">
                Deleting is permanent. We remove your profiles, the items you track, dose logs, check-ins, protocols, notes, alert settings, sign-in details, and sessions. We can’t bring them back, so download a copy first if you want one.
              </p>
              {removable.length > 0 && (
                <p className="mt-3 text-sm text-white/50">
                  Right now that includes {removable.map(table => `${table.count} ${label(table.name).toLowerCase()}`).slice(0, 6).join(', ')}{removable.length > 6 ? ', and more' : ''}.
                </p>
              )}
              <div className="mt-4 rounded-xl border border-white/10 bg-black/20 p-4 text-sm leading-6 text-white/55">
                <p className="font-semibold text-white/75">What stays, and for how long</p>
                <ul className="mt-2 list-disc space-y-1 pl-5">
                  <li>Backups. Our database backups age out within {days} days, so deleted data can remain in a backup for up to {days} days. We only use backups to recover from a failure.</li>
                  <li>If you have a paid plan, we cancel it. Stripe keeps its own billing records where tax rules require it.</li>
                  <li>A small record that a deletion happened: a random id, the date, and a row count. Nothing in it identifies you.</li>
                  {kept.length > 0 && <li>Governance receipts hold only a random account id, with no name, email, or health values. After deletion that id points to nothing.</li>}
                </ul>
              </div>
              <p className="mt-4 text-sm text-white/55">
                Step 1: type <span className="font-mono font-semibold text-white/80">{required}</span> below. Step 2: we email you a one-time link, and you open it in this browser to finish.
              </p>
              <label className="mt-3 block text-sm text-white/65" htmlFor="delete-phrase">Type the phrase to continue</label>
              <input
                id="delete-phrase"
                value={phrase}
                onChange={event => setPhrase(event.target.value)}
                autoComplete="off"
                spellCheck={false}
                className="mt-1 min-h-11 w-full rounded-xl border border-white/10 bg-black/20 px-4 text-sm text-white outline-none focus:border-red-300/45"
              />
              <button type="button" disabled={requesting || phrase.trim() !== required} onClick={() => void requestDeletion()}
                className="mt-4 min-h-11 rounded-xl bg-red-500 px-5 text-sm font-bold text-white hover:bg-red-400 disabled:opacity-45">
                {requesting ? 'Sending…' : 'Email me a confirmation link'}
              </button>
              {deleteMessage && <p className="mt-4 text-sm text-red-100/80" role="alert">{deleteMessage}</p>}
            </>
          )}
        </section>
      </div>
    </div>
  );
}
