'use client';

import { BioStackLogo } from '@/components/ui/BioStackLogo';
import { useSearchParams } from 'next/navigation';
import { FormEvent, Suspense, useEffect, useMemo, useState } from 'react';

const API_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';

function resolveRedirectPath(callbackUrl: string | null) {
  if (!callbackUrl) {
    return '/mission-control';
  }

  if (callbackUrl.startsWith('/')) {
    return callbackUrl;
  }

  try {
    const parsed = new URL(callbackUrl);
    return `${parsed.pathname}${parsed.search}${parsed.hash}` || '/mission-control';
  } catch {
    return '/mission-control';
  }
}

function maskEmail(email: string) {
  const [name, domain] = email.split('@');
  if (!name || !domain) {
    return email;
  }

  const visible = name.length <= 2 ? name[0] : `${name[0]}${name[name.length - 1]}`;
  return `${visible}${'*'.repeat(Math.max(2, name.length - visible.length))}@${domain}`;
}

function SignInPageContent() {
  const searchParams = useSearchParams();
  const redirectPath = useMemo(() => resolveRedirectPath(searchParams.get('callbackUrl')), [searchParams]);
  const error = searchParams.get('error');
  const [email, setEmail] = useState('');
  const [submittedEmail, setSubmittedEmail] = useState('');
  const [isSending, setIsSending] = useState(false);
  const [sendError, setSendError] = useState('');
  const [cooldownUntil, setCooldownUntil] = useState(0);
  const [now, setNow] = useState(Date.now());

  const isInboxStep = Boolean(submittedEmail);
  const cooldownMs = Math.max(0, cooldownUntil - now);
  const cooldownSeconds = Math.ceil(cooldownMs / 1000);

  useEffect(() => {
    if (!cooldownUntil) {
      return;
    }

    const id = window.setInterval(() => {
      const nextNow = Date.now();
      setNow(nextNow);
      if (nextNow >= cooldownUntil) {
        setCooldownUntil(0);
      }
    }, 1000);

    return () => window.clearInterval(id);
  }, [cooldownUntil]);

  async function startAuth(nextEmail = email) {
    const normalized = nextEmail.trim().toLowerCase();
    if (!normalized || cooldownMs > 0) {
      return;
    }

    setIsSending(true);
    setSendError('');
    try {
      const response = await fetch(`${API_URL}/api/v1/auth/start`, {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ contact: normalized, channel: 'email', redirectPath }),
      });

      if (!response.ok) {
        throw new Error('Unable to send sign-in link.');
      }

      setSubmittedEmail(normalized);
      setCooldownUntil(Date.now() + 30000);
    } catch {
      setSendError('We could not send that sign-in link. Try again in a moment.');
    } finally {
      setIsSending(false);
    }
  }

  function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    void startAuth();
  }

  return (
    <main className="min-h-screen bg-[#0B0F14] px-4 py-8 text-white/90">
      <div className="mx-auto flex min-h-[calc(100vh-4rem)] w-full max-w-md flex-col justify-center">
        <div className="mb-8 flex justify-center">
          <BioStackLogo variant="stacked" theme="dark" size="lg" />
        </div>

        <section className="rounded-lg border border-white/[0.07] bg-white/[0.035] p-6 shadow-2xl sm:p-8">
          {!isInboxStep ? (
            <>
              <div className="mb-7 text-center">
                <h1 className="text-2xl font-bold tracking-tight text-white">Sign in to BioStack</h1>
                <p className="mt-2 text-sm leading-6 text-white/45">
                  Use your email. We will send a private sign-in link.
                </p>
              </div>

              {error && (
                <div className="mb-5 rounded-lg border border-red-300/20 bg-red-500/10 px-4 py-3 text-sm text-red-100/80">
                  That sign-in link is expired or already used. Send yourself a new one.
                </div>
              )}

              {sendError && (
                <div className="mb-5 rounded-lg border border-red-300/20 bg-red-500/10 px-4 py-3 text-sm text-red-100/80">
                  {sendError}
                </div>
              )}

              <form onSubmit={onSubmit} className="space-y-4">
                <label className="block">
                  <span className="mb-2 block text-sm font-semibold text-white/70">Email</span>
                  <input
                    type="email"
                    name="email"
                    autoComplete="email"
                    inputMode="email"
                    required
                    value={email}
                    onChange={(event) => setEmail(event.target.value)}
                    className="min-h-12 w-full rounded-lg border border-white/10 bg-black/25 px-4 text-base text-white outline-none transition-colors placeholder:text-white/25 focus:border-emerald-300/50"
                    placeholder="you@example.com"
                  />
                </label>
                <button
                  type="submit"
                  disabled={isSending}
                  className="min-h-12 w-full rounded-lg bg-emerald-400 px-5 text-sm font-bold text-[#07110c] transition-colors hover:bg-emerald-300 disabled:cursor-not-allowed disabled:opacity-65"
                >
                  {isSending ? 'Sending...' : 'Continue'}
                </button>
              </form>
            </>
          ) : (
            <div className="text-center">
              <h1 className="text-2xl font-bold tracking-tight text-white">Check your inbox</h1>
              <p className="mt-3 text-sm leading-6 text-white/50">
                We sent a sign-in link to <span className="font-semibold text-white/80">{maskEmail(submittedEmail)}</span>.
              </p>
              <p className="mt-4 text-sm text-white/35">Open the link on this device to continue.</p>

              <button
                type="button"
                disabled={isSending || cooldownMs > 0}
                onClick={() => void startAuth(submittedEmail)}
                className="mt-7 min-h-12 w-full rounded-lg border border-white/10 bg-white/[0.04] px-5 text-sm font-semibold text-white/75 transition-colors hover:bg-white/[0.07] disabled:cursor-not-allowed disabled:opacity-55"
              >
                {cooldownMs > 0 ? `Resend in ${cooldownSeconds}s` : 'Resend link'}
              </button>
            </div>
          )}
        </section>
      </div>
    </main>
  );
}

export default function SignInPage() {
  return (
    <Suspense fallback={<main className="min-h-screen bg-[#0B0F14]" />}>
      <SignInPageContent />
    </Suspense>
  );
}
