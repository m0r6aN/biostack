'use client';

import { BioStackLogo } from '@/components/ui/BioStackLogo';
import { useSearchParams } from 'next/navigation';
import { Suspense, useEffect, useRef } from 'react';

const API_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';

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
 */
function VerifyPageContent() {
  const searchParams = useSearchParams();
  const token = searchParams.get('token');
  const redirected = useRef(false);

  useEffect(() => {
    if (!token || redirected.current) {
      return;
    }
    redirected.current = true;

    // Navigate the browser to the backend verify endpoint.
    // The backend will set the session cookie and redirect back to the app.
    window.location.href = `${API_URL}/auth/verify?token=${encodeURIComponent(token)}`;
  }, [token]);

  return (
    <main className="min-h-screen bg-[#0B0F14] px-4 py-8 text-white/90">
      <div className="mx-auto flex min-h-[calc(100vh-4rem)] w-full max-w-md flex-col justify-center">
        <div className="mb-8 flex justify-center">
          <BioStackLogo variant="stacked" theme="dark" size="lg" />
        </div>

        <section className="rounded-lg border border-white/[0.07] bg-white/[0.035] p-6 shadow-2xl sm:p-8 text-center">
          {token ? (
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
