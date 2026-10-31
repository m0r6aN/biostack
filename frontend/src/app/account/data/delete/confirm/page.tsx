'use client';

import { Header } from '@/components/Header';
import { getApiBaseUrl } from '@/lib/apiBase';
import Link from 'next/link';
import { useEffect, useRef, useState } from 'react';

const API_URL = getApiBaseUrl();

type Receipt = {
  receiptId: string;
  deletedOnUtc: string;
  backupsAgeOutByUtc: string;
  backupRetentionDays: number;
  receiptEmailQueued: boolean;
};

const day = (value: string) => value.slice(0, 10);

/**
 * Last step of account deletion. The emailed link lands here with a one-time token. The token is read once,
 * removed from the address bar, and only sent when the person presses the button, so a link scanner that
 * merely opens the page cannot delete anything.
 */
export default function ConfirmAccountDeletionPage() {
  const [token, setToken] = useState<string | null>(null);
  const [state, setState] = useState<'checking' | 'ready' | 'missing' | 'working' | 'deleted'>('checking');
  const [error, setError] = useState('');
  const [receipt, setReceipt] = useState<Receipt | null>(null);
  const read = useRef(false);

  useEffect(() => {
    if (read.current) return;
    read.current = true;
    const fromQuery = new URLSearchParams(window.location.search).get('token');
    const fromFragment = new URLSearchParams(window.location.hash.replace(/^#/, '')).get('token');
    const found = fromQuery ?? fromFragment;
    window.history.replaceState(null, '', window.location.pathname);
    setToken(found);
    setState(found ? 'ready' : 'missing');
  }, []);

  async function confirm() {
    if (!token) return;
    setState('working');
    setError('');
    try {
      const response = await fetch(`${API_URL}/api/v1/account/deletion/confirm`, {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ token }),
      });
      if (response.ok) {
        setReceipt(await response.json() as Receipt);
        setState('deleted');
        return;
      }
      if (response.status === 401) {
        setError('You’re not signed in in this browser. Sign in with the same email, then open the link from your email again.');
      } else {
        const body = await response.json().catch(() => ({})) as { message?: string };
        setError(body.message ?? 'We couldn’t delete your account. Try again.');
      }
      setState('ready');
    } catch {
      setError('We couldn’t reach BioStack. Nothing was deleted. Check your connection and try again.');
      setState('ready');
    }
  }

  return (
    <div className="min-h-full">
      <Header title="Confirm account deletion" subtitle="This is the last step" />
      <div className="mx-auto max-w-2xl space-y-6 p-5 sm:p-8">
        {state === 'deleted' && receipt ? (
          <section className="rounded-2xl border border-emerald-300/20 bg-emerald-400/[0.05] p-6" role="status">
            <h2 className="text-lg font-semibold text-white">Your account has been deleted</h2>
            <dl className="mt-4 space-y-2 text-sm text-white/70">
              <div><dt className="inline text-white/50">Deleted on: </dt><dd className="inline">{day(receipt.deletedOnUtc)} (UTC)</dd></div>
              <div><dt className="inline text-white/50">Receipt id: </dt><dd className="inline font-mono">{receipt.receiptId}</dd></div>
              <div>
                <dt className="inline text-white/50">Backups: </dt>
                <dd className="inline">they age out within {receipt.backupRetentionDays} days, by {day(receipt.backupsAgeOutByUtc)}.</dd>
              </div>
            </dl>
            <p className="mt-4 text-sm leading-6 text-white/55">
              {receipt.receiptEmailQueued
                ? 'We sent a receipt to your email address.'
                : 'We couldn’t send the receipt email. Keep this page or the receipt id above.'}
            </p>
            {/* A full page load, so the signed-in state held by the app shell is dropped. */}
            <Link href="/" onClick={event => { event.preventDefault(); window.location.assign('/'); }} className="mt-5 inline-flex min-h-11 items-center rounded-xl border border-white/20 px-5 text-sm font-semibold text-white hover:bg-white/10">
              Back to BioStack
            </Link>
          </section>
        ) : state === 'checking' ? (
          <p className="text-sm text-white/60" role="status">Checking your link…</p>
        ) : state === 'missing' ? (
          <section className="rounded-2xl border border-white/[0.07] bg-white/[0.025] p-6">
            <p className="text-sm leading-6 text-white/65">This page needs the link from your confirmation email. Request a new one from Your data.</p>
            <Link href="/account/data" className="mt-4 inline-flex min-h-11 items-center rounded-xl border border-white/20 px-5 text-sm font-semibold text-white hover:bg-white/10">
              Go to Your data
            </Link>
          </section>
        ) : (
          <section className="rounded-2xl border border-red-300/15 bg-red-400/[0.04] p-6">
            <h2 className="text-lg font-semibold text-white">Delete your account permanently?</h2>
            <p className="mt-2 text-sm leading-6 text-white/60">
              This removes your profiles, items, dose logs, check-ins, protocols, notes, alert settings, sign-in details, and sessions. We can’t bring them back. Backups age out within 35 days.
            </p>
            <div className="mt-5 flex flex-col gap-3 sm:flex-row">
              <button type="button" disabled={state === 'working'} onClick={() => void confirm()}
                className="min-h-11 rounded-xl bg-red-500 px-5 text-sm font-bold text-white hover:bg-red-400 disabled:opacity-45">
                {state === 'working' ? 'Deleting…' : 'Permanently delete my account'}
              </button>
              <Link href="/account/data" className="inline-flex min-h-11 items-center justify-center rounded-xl border border-white/20 px-5 text-sm font-semibold text-white hover:bg-white/10">
                Keep my account
              </Link>
            </div>
            {error && <p className="mt-4 text-sm text-red-100/85" role="alert">{error}</p>}
          </section>
        )}
      </div>
    </div>
  );
}
