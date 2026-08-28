'use client';

import { Header } from '@/components/Header';
import { getApiBaseUrl } from '@/lib/apiBase';
import { passkeysSupported, registerPasskey } from '@/lib/passkeys';
import { useCallback, useEffect, useState } from 'react';

const API_URL = getApiBaseUrl();

type PasskeySummary = {
  id: string;
  displayName: string;
  transports: string[];
  isBackupEligible: boolean;
  isBackedUp: boolean;
  createdAtUtc: string;
  lastUsedAtUtc: string | null;
};

export default function AccountSecurityPage() {
  const [enabled, setEnabled] = useState(false);
  const [credentials, setCredentials] = useState<PasskeySummary[]>([]);
  const [displayName, setDisplayName] = useState('My passkey');
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState('');

  const load = useCallback(async () => {
    const statusResponse = await fetch(`${API_URL}/api/v1/auth/passkeys/status`, {
      credentials: 'include',
      cache: 'no-store',
    });
    const status = statusResponse.ok ? await statusResponse.json() as { enabled?: boolean } : null;
    setEnabled(status?.enabled === true);
    if (status?.enabled !== true) {
      setCredentials([]);
      return;
    }

    const response = await fetch(`${API_URL}/api/v1/auth/passkeys`, {
      credentials: 'include',
      cache: 'no-store',
    });
    if (response.ok) {
      setCredentials(await response.json() as PasskeySummary[]);
    }
  }, []);

  useEffect(() => {
    // The request synchronizes this client surface with server-held credential state.
    // eslint-disable-next-line react-hooks/set-state-in-effect
    void load().catch(() => setMessage('Passkey settings could not be loaded.'));
  }, [load]);

  async function addPasskey() {
    setBusy(true);
    setMessage('');
    try {
      await registerPasskey(displayName);
      await load();
      setMessage('Passkey added. You can use it the next time you sign in.');
    } catch {
      setMessage('The passkey was not added. Your authenticator may have cancelled or rejected the request.');
    } finally {
      setBusy(false);
    }
  }

  async function removePasskey(credential: PasskeySummary) {
    if (!window.confirm(`Remove “${credential.displayName}”? Email recovery will remain available.`)) {
      return;
    }
    setBusy(true);
    setMessage('');
    try {
      const response = await fetch(`${API_URL}/api/v1/auth/passkeys/${credential.id}`, {
        method: 'DELETE',
        credentials: 'include',
      });
      if (!response.ok) {
        throw new Error('remove-failed');
      }
      await load();
      setMessage('Passkey removed.');
    } catch {
      setMessage('That passkey could not be removed. Confirm that verified email recovery is still available.');
    } finally {
      setBusy(false);
    }
  }

  const browserSupported = passkeysSupported();

  return (
    <div className="min-h-full">
      <Header title="Account security" subtitle="Passkeys and recovery" />
      <main className="mx-auto max-w-3xl space-y-6 p-5 sm:p-8">
        <section className="rounded-2xl border border-white/[0.07] bg-white/[0.025] p-6">
          <h2 className="text-lg font-semibold text-white">Passkeys</h2>
          <p className="mt-2 text-sm leading-6 text-white/45">
            Passkeys use your device screen lock, fingerprint, or face verification. BioStack keeps the public key only; your private key stays with your authenticator.
          </p>

          {!enabled ? (
            <p className="mt-5 rounded-xl border border-amber-300/15 bg-amber-300/[0.05] p-4 text-sm text-amber-100/70">
              Passkeys are not enabled for this deployment. Email sign-in and recovery are unchanged.
            </p>
          ) : !browserSupported ? (
            <p className="mt-5 rounded-xl border border-white/10 bg-white/[0.03] p-4 text-sm text-white/55">
              This browser does not expose WebAuthn passkey support. You can continue using email links.
            </p>
          ) : (
            <div className="mt-5 flex flex-col gap-3 sm:flex-row">
              <input
                value={displayName}
                maxLength={100}
                onChange={event => setDisplayName(event.target.value)}
                aria-label="Passkey name"
                className="min-h-11 flex-1 rounded-xl border border-white/10 bg-black/20 px-4 text-sm text-white outline-none focus:border-emerald-300/45"
              />
              <button
                type="button"
                disabled={busy || !displayName.trim()}
                onClick={() => void addPasskey()}
                className="min-h-11 rounded-xl bg-emerald-400 px-5 text-sm font-bold text-[#07110c] hover:bg-emerald-300 disabled:opacity-55"
              >
                {busy ? 'Working…' : 'Add passkey'}
              </button>
            </div>
          )}

          {message && <p className="mt-4 text-sm text-white/60" role="status">{message}</p>}
        </section>

        {enabled && (
          <section className="rounded-2xl border border-white/[0.07] bg-white/[0.025] p-6">
            <h2 className="text-lg font-semibold text-white">Your passkeys</h2>
            {credentials.length === 0 ? (
              <p className="mt-3 text-sm text-white/40">No passkeys enrolled yet.</p>
            ) : (
              <div className="mt-4 divide-y divide-white/[0.06]">
                {credentials.map(credential => (
                  <div key={credential.id} className="flex items-center justify-between gap-4 py-4 first:pt-0 last:pb-0">
                    <div>
                      <p className="text-sm font-semibold text-white/80">{credential.displayName}</p>
                      <p className="mt-1 text-xs text-white/35">
                        Added {new Date(credential.createdAtUtc).toLocaleDateString()}
                        {credential.isBackedUp ? ' · synced backup reported' : ''}
                      </p>
                    </div>
                    <button
                      type="button"
                      disabled={busy}
                      onClick={() => void removePasskey(credential)}
                      className="rounded-lg border border-red-300/15 px-3 py-2 text-xs font-semibold text-red-200/70 hover:bg-red-400/10 disabled:opacity-50"
                    >
                      Remove
                    </button>
                  </div>
                ))}
              </div>
            )}
          </section>
        )}

        <section className="rounded-2xl border border-white/[0.07] bg-white/[0.025] p-6">
          <h2 className="text-lg font-semibold text-white">Email recovery</h2>
          <p className="mt-2 text-sm leading-6 text-white/45">
            Verified email magic links remain available for registration, recovery, and fallback. Each link expires after 15 minutes and can be used once.
          </p>
        </section>
      </main>
    </div>
  );
}
