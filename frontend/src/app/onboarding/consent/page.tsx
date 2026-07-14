'use client';

import { BioStackLogo } from '@/components/ui/BioStackLogo';
import { canonicalRoutes } from '@/lib/productContract';
import { useSearchParams } from 'next/navigation';
import { Suspense, useEffect, useMemo, useState } from 'react';

type ConsentStatus = {
  accepted: boolean;
  declined: boolean;
  currentVersion: string;
};

function normalizeReturnTo(value: string | null) {
  if (!value || !value.startsWith('/') || value.startsWith('//') || value.includes('\\')) {
    return canonicalRoutes.postSignInDefault;
  }

  return value;
}

function ConsentPageContent() {
  const searchParams = useSearchParams();
  const returnTo = useMemo(() => normalizeReturnTo(searchParams.get('returnTo')), [searchParams]);
  const [status, setStatus] = useState<ConsentStatus | null>(null);
  const [error, setError] = useState('');
  const [submitting, setSubmitting] = useState<'accept' | 'decline' | null>(null);

  useEffect(() => {
    let active = true;

    void fetch('/api/v1/consent', { credentials: 'include', cache: 'no-store' })
      .then(async (response) => {
        if (response.status === 401) {
          const callbackUrl = `/onboarding/consent?returnTo=${encodeURIComponent(returnTo)}`;
          window.location.replace(`/auth/signin?callbackUrl=${encodeURIComponent(callbackUrl)}`);
          return null;
        }
        if (!response.ok) {
          throw new Error('consent-status-unavailable');
        }
        return (await response.json()) as ConsentStatus;
      })
      .then((nextStatus) => {
        if (!active || !nextStatus) {
          return;
        }
        if (nextStatus.accepted) {
          window.location.replace(returnTo);
          return;
        }
        setStatus(nextStatus);
      })
      .catch(() => {
        if (active) {
          setError('We could not load the consent record. Please try again.');
        }
      });

    return () => {
      active = false;
    };
  }, [returnTo]);

  async function accept() {
    setSubmitting('accept');
    setError('');
    try {
      const response = await fetch('/api/v1/consent', {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ consentVersion: status?.currentVersion }),
      });
      if (!response.ok) {
        throw new Error('consent-accept-failed');
      }
      window.location.replace(returnTo);
    } catch {
      setError('We could not record your choice. Please try again.');
      setSubmitting(null);
    }
  }

  async function decline() {
    setSubmitting('decline');
    setError('');
    try {
      const response = await fetch('/api/v1/consent/decline', {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ consentVersion: status?.currentVersion }),
      });
      if (!response.ok) {
        throw new Error('consent-decline-failed');
      }
      await fetch('/api/v1/auth/logout', { method: 'POST', credentials: 'include' }).catch(() => undefined);
      window.location.replace('/?consent=declined');
    } catch {
      setError('We could not record your choice. Please try again.');
      setSubmitting(null);
    }
  }

  return (
    <main className="min-h-screen bg-[#0B0F14] px-4 py-8 text-white/90">
      <div className="mx-auto flex min-h-[calc(100vh-4rem)] w-full max-w-xl flex-col justify-center">
        <div className="mb-8 flex justify-center">
          <BioStackLogo variant="stacked" theme="dark" size="lg" />
        </div>

        <section className="rounded-lg border border-white/[0.07] bg-white/[0.035] p-6 shadow-2xl sm:p-8">
          <p className="text-xs font-semibold uppercase tracking-[0.22em] text-emerald-300/70">Before you save data</p>
          <h1 className="mt-3 text-2xl font-bold tracking-tight text-white">Choose whether to continue</h1>
          <div className="mt-5 space-y-3 text-sm leading-6 text-white/62">
            <p>BioStack stores the profile, compounds, goals, and observations you choose to save.</p>
            <p>Its outputs are observational and informational. They are not medical advice, diagnosis, or treatment instructions.</p>
            <p>You can choose not to continue. Your unsaved preview remains on this device unless you clear browser storage.</p>
          </div>

          {status?.declined && (
            <p className="mt-5 rounded-lg border border-amber-300/18 bg-amber-400/[0.07] px-4 py-3 text-sm text-amber-50/80">
              You previously chose not to continue with this consent version. You may review it again now.
            </p>
          )}

          {error && (
            <p className="mt-5 rounded-lg border border-red-300/20 bg-red-500/10 px-4 py-3 text-sm text-red-100/80">
              {error}
            </p>
          )}

          <div className="mt-7 grid gap-3 sm:grid-cols-2">
            <button
              type="button"
              onClick={() => void accept()}
              disabled={!status || submitting !== null}
              className="min-h-12 rounded-lg bg-emerald-400 px-5 text-sm font-bold text-[#07110c] transition-colors hover:bg-emerald-300 disabled:cursor-not-allowed disabled:opacity-60"
            >
              {submitting === 'accept' ? 'Recording...' : 'I agree and want to continue'}
            </button>
            <button
              type="button"
              onClick={() => void decline()}
              disabled={!status || submitting !== null}
              className="min-h-12 rounded-lg border border-white/10 bg-white/[0.04] px-5 text-sm font-semibold text-white/75 transition-colors hover:bg-white/[0.07] disabled:cursor-not-allowed disabled:opacity-60"
            >
              {submitting === 'decline' ? 'Recording...' : 'Not now'}
            </button>
          </div>

          {status?.currentVersion && (
            <p className="mt-5 text-center text-xs text-white/30">Consent record: {status.currentVersion}</p>
          )}
        </section>
      </div>
    </main>
  );
}

export default function ConsentPage() {
  return (
    <Suspense fallback={<main className="min-h-screen bg-[#0B0F14]" />}>
      <ConsentPageContent />
    </Suspense>
  );
}
