'use client';

import { BioStackLogo } from '@/components/ui/BioStackLogo';
import { passkeysSupported, registerPasskey } from '@/lib/passkeys';
import { useSearchParams } from 'next/navigation';
import { Suspense, useEffect, useRef, useState } from 'react';

/**
 * Magic-link landing page.
 *
 * The magic link in the email points HERE (frontend), not directly to the backend
 * /auth/verify endpoint. This prevents email clients (Gmail, Outlook, etc.) from
 * pre-fetching and consuming the one-time token with their link-scanner crawlers.
 *
 * Email scanners see static HTML with no interactivity — they don't execute JavaScript.
 * The actual token exchange only happens when a real browser runs this component and
 * navigates to the backend endpoint.
 *
 * After a successful exchange this page also makes the one post-sign-in offer to
 * enroll a passkey — the moment of highest intent. The offer only appears when the
 * deployment has passkeys enabled, the browser supports WebAuthn, the account has no
 * passkey yet, the user hasn't dismissed the offer on this device, and the redirect
 * is not into onboarding (new users finish onboarding first). Any failure in the
 * eligibility checks falls through to the normal redirect — the offer never blocks
 * sign-in.
 */

const NUDGE_DISMISSED_KEY = 'biostack.passkeyNudgeDismissed';

function readNudgeDismissed(): boolean {
  try {
    return window.localStorage.getItem(NUDGE_DISMISSED_KEY) === '1';
  } catch {
    return false;
  }
}

function writeNudgeDismissed() {
  try {
    window.localStorage.setItem(NUDGE_DISMISSED_KEY, '1');
  } catch {
    // Storage unavailable — the user may see the offer again; never block sign-in.
  }
}

function VerifyPageContent() {
  const searchParams = useSearchParams();
  const queryToken = searchParams.get('token');
  const redirected = useRef(false);
  const [tokenState, setTokenState] = useState<'checking' | 'present' | 'missing'>('checking');
  const [passkeyOffer, setPasskeyOffer] = useState<{ redirectPath: string } | null>(null);
  const [enrolling, setEnrolling] = useState(false);
  const [enrollError, setEnrollError] = useState('');

  useEffect(() => {
    const fragmentToken = new URLSearchParams(window.location.hash.replace(/^#/, '')).get('token');
    const token = queryToken ?? fragmentToken;
    if (!token || redirected.current) {
      setTokenState('missing');
      return;
    }
    setTokenState('present');
    redirected.current = true;

    // Remove the one-time token from browser history before exchanging it. The
    // exchange is a POST so link scanners cannot consume the token with a GET.
    window.history.replaceState(null, '', '/auth/verify');

    async function shouldOfferPasskey(redirectPath: string): Promise<boolean> {
      if (redirectPath.startsWith('/onboarding') || !passkeysSupported() || readNudgeDismissed()) {
        return false;
      }
      try {
        const statusResponse = await fetch('/api/v1/auth/passkeys/status', {
          credentials: 'include',
          cache: 'no-store',
        });
        if (!statusResponse.ok) {
          return false;
        }
        const status = (await statusResponse.json()) as { enabled?: boolean };
        if (status?.enabled !== true) {
          return false;
        }
        const listResponse = await fetch('/api/v1/auth/passkeys', {
          credentials: 'include',
          cache: 'no-store',
        });
        if (!listResponse.ok) {
          return false;
        }
        const existing = (await listResponse.json()) as unknown[];
        return Array.isArray(existing) && existing.length === 0;
      } catch {
        return false;
      }
    }

    void fetch('/api/v1/auth/verify', {
      method: 'POST',
      credentials: 'include',
      cache: 'no-store',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ token }),
    })
      .then(async (response) => {
        if (!response.ok) {
          throw new Error('invalid-link');
        }

        const result = (await response.json()) as { redirectPath?: string };
        const redirectPath = result.redirectPath;
        if (!redirectPath?.startsWith('/') || redirectPath.startsWith('//') || redirectPath.includes('\\')) {
          throw new Error('invalid-return-path');
        }

        if (await shouldOfferPasskey(redirectPath)) {
          setPasskeyOffer({ redirectPath });
          return;
        }

        window.location.replace(redirectPath);
      })
      .catch(() => window.location.replace('/auth/signin?error=invalid-link'));
  }, [queryToken]);

  async function enrollPasskey(redirectPath: string) {
    setEnrolling(true);
    setEnrollError('');
    try {
      await registerPasskey('My passkey');
      window.location.replace(redirectPath);
    } catch {
      setEnrollError('That did not go through — your authenticator may have cancelled. You can try again, skip for now, or add one later under Account security.');
      setEnrolling(false);
    }
  }

  function skipPasskey(redirectPath: string) {
    writeNudgeDismissed();
    window.location.replace(redirectPath);
  }

  return (
    <main className="min-h-screen bg-[#0B0F14] px-4 py-8 text-white/90">
      <div className="mx-auto flex min-h-[calc(100vh-4rem)] w-full max-w-md flex-col justify-center">
        <div className="mb-8 flex justify-center">
          <BioStackLogo variant="stacked" theme="dark" size="lg" />
        </div>

        <section className="rounded-lg border border-white/[0.07] bg-white/[0.035] p-6 shadow-2xl sm:p-8 text-center">
          {passkeyOffer ? (
            <>
              <h1 className="text-xl font-bold tracking-tight text-white">You&apos;re signed in</h1>
              <p className="mt-3 text-sm leading-6 text-white/50">
                Add a passkey and next time you can skip the email — sign in with your
                device&apos;s fingerprint, face, or screen lock.
              </p>
              {enrollError && (
                <p className="mt-4 rounded-lg border border-red-300/20 bg-red-500/10 px-4 py-3 text-left text-sm text-red-100/80" role="status">
                  {enrollError}
                </p>
              )}
              <button
                type="button"
                disabled={enrolling}
                onClick={() => void enrollPasskey(passkeyOffer.redirectPath)}
                className="mt-6 min-h-12 w-full rounded-lg bg-emerald-400 px-5 text-sm font-bold text-[#07110c] transition-colors hover:bg-emerald-300 disabled:cursor-not-allowed disabled:opacity-65"
              >
                {enrolling ? 'Waiting for your authenticator…' : 'Add a passkey'}
              </button>
              <button
                type="button"
                disabled={enrolling}
                onClick={() => skipPasskey(passkeyOffer.redirectPath)}
                className="mt-3 min-h-12 w-full rounded-lg border border-white/10 bg-white/[0.04] px-5 text-sm font-semibold text-white/70 transition-colors hover:bg-white/[0.07] disabled:cursor-not-allowed disabled:opacity-55"
              >
                Not now
              </button>
              <p className="mt-4 text-xs leading-5 text-white/35">
                Email sign-in links always remain available for recovery.
              </p>
            </>
          ) : tokenState !== 'missing' ? (
            <>
              <div className="mb-4 flex justify-center">
                {/* Spinner */}
                <div className="h-8 w-8 animate-spin rounded-full border-2 border-white/10 border-t-emerald-400" />
              </div>
              <h1 className="text-xl font-bold tracking-tight text-white">Signing you in…</h1>
              <p className="mt-2 text-sm text-white/45">Hang on while we verify your link.</p>
            </>
          ) : (
            <>
              <h1 className="text-xl font-bold tracking-tight text-white">Invalid link</h1>
              <p className="mt-2 text-sm text-white/45">
                This sign-in link is missing a token.{' '}
                <a href="/auth/signin" className="text-emerald-400 underline-offset-2 hover:underline">
                  Request a new one.
                </a>
              </p>
            </>
          )}
        </section>
      </div>
    </main>
  );
}

export default function VerifyPage() {
  return (
    <Suspense fallback={<main className="min-h-screen bg-[#0B0F14]" />}>
      <VerifyPageContent />
    </Suspense>
  );
}
