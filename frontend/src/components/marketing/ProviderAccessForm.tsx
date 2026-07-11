'use client';

import { apiClient } from '@/lib/api';
import { useState } from 'react';
import type { FormEvent } from 'react';

export function ProviderAccessForm() {
  const [state, setState] = useState<'idle' | 'submitting' | 'confirmed'>('idle');
  const [error, setError] = useState('');

  const submit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    setState('submitting');
    setError('');

    try {
      await apiClient.requestProviderAccess({
        email: String(form.get('email') ?? ''),
        name: String(form.get('name') ?? ''),
        organization: String(form.get('organization') ?? ''),
        role: String(form.get('role') ?? ''),
        consent: form.get('consent') === 'on',
        website: String(form.get('website') ?? ''),
      });
      setState('confirmed');
    } catch (cause) {
      setState('idle');
      setError(cause instanceof Error ? cause.message : 'Your request could not be submitted. Please try again.');
    }
  };

  if (state === 'confirmed') {
    return (
      <div role="status" className="rounded-lg border border-emerald-300/20 bg-emerald-400/[0.08] p-6">
        <h2 className="text-xl font-semibold text-emerald-50">Request received</h2>
        <p className="mt-3 text-sm leading-6 text-emerald-50/72">
          Your provider pilot request is in the review queue. BioStack will use the contact details you supplied only to follow up about provider access.
        </p>
      </div>
    );
  }

  return (
    <form onSubmit={submit} className="rounded-lg border border-white/10 bg-white/[0.035] p-6">
      <h2 className="text-xl font-semibold text-white">Request provider pilot access</h2>
      <p className="mt-2 text-sm leading-6 text-white/55">
        Contact information only. Do not include client, health, compound, or protocol details.
      </p>

      <div className="mt-5 grid gap-4 sm:grid-cols-2">
        <Field label="Name" name="name" autoComplete="name" />
        <Field label="Work email" name="email" type="email" autoComplete="email" />
        <Field label="Organization" name="organization" autoComplete="organization" />
        <Field label="Role" name="role" autoComplete="organization-title" />
      </div>

      <div className="sr-only" aria-hidden="true">
        <label htmlFor="provider-website">Website</label>
        <input id="provider-website" name="website" tabIndex={-1} autoComplete="off" />
      </div>

      <label className="mt-5 flex items-start gap-3 text-sm leading-6 text-white/62">
        <input name="consent" type="checkbox" required className="mt-1 h-4 w-4" />
        <span>I agree that BioStack may store these contact details and contact me about the provider pilot.</span>
      </label>

      {error && <p role="alert" className="mt-4 text-sm text-red-200">{error}</p>}

      <button
        type="submit"
        disabled={state === 'submitting'}
        className="mt-5 rounded-lg bg-emerald-400 px-5 py-3 text-sm font-semibold text-slate-950 transition-colors hover:bg-emerald-300 disabled:opacity-60"
      >
        {state === 'submitting' ? 'Submitting...' : 'Request provider access'}
      </button>
    </form>
  );
}

function Field({
  label,
  name,
  type = 'text',
  autoComplete,
}: {
  label: string;
  name: string;
  type?: string;
  autoComplete: string;
}) {
  return (
    <label className="text-sm font-medium text-white/72">
      {label}
      <input
        name={name}
        type={type}
        required
        maxLength={name === 'email' ? 255 : 200}
        autoComplete={autoComplete}
        className="mt-2 w-full rounded-lg border border-white/12 bg-[#0f151e] px-4 py-3 text-white outline-none focus:border-emerald-300/45"
      />
    </label>
  );
}
