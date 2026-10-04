'use client';

import { Header } from '@/components/Header';
import { GlassCard } from '@/components/ui/GlassCard';
import { getApiBaseUrl } from '@/lib/apiBase';
import { FormEvent, useState } from 'react';

const CATEGORIES = ['Support', 'Billing', 'Feedback', 'Partnership', 'Other'] as const;

type ContactStatus = 'idle' | 'submitting' | 'success';

const SEND_FAILED_MESSAGE = 'We couldn’t send your message right now. Please try again.';

export default function ContactPage() {
  const [name, setName] = useState('');
  const [email, setEmail] = useState('');
  const [category, setCategory] = useState<(typeof CATEGORIES)[number]>('Support');
  const [message, setMessage] = useState('');
  const [status, setStatus] = useState<ContactStatus>('idle');
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setStatus('submitting');
    setError(null);

    try {
      const res = await fetch(`${getApiBaseUrl()}/api/v1/contact`, {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ name, email, category, message }),
      });

      if (!res.ok) {
        const body = (await res.json().catch(() => null)) as { error?: string } | null;
        setError(body?.error ?? SEND_FAILED_MESSAGE);
        setStatus('idle');
        return;
      }

      setStatus('success');
    } catch {
      setError(SEND_FAILED_MESSAGE);
      setStatus('idle');
    }
  };

  return (
    <div className="flex-1 flex flex-col min-h-screen bg-[#0B0F14]">
      <Header title="Contact Us" subtitle="We read every message" />

      <main id="main" tabIndex={-1} className="flex-1 p-6 w-full max-w-2xl mx-auto">
        {status === 'success' ? (
          <GlassCard className="p-8 space-y-4">
            <h2 className="text-xl font-semibold text-white">Message sent</h2>
            <p className="text-sm leading-6 text-white/60">
              Thanks for reaching out — we read every message and will get back to you at{' '}
              <span className="font-semibold text-white/80">{email}</span>.
            </p>
            <p className="text-sm text-white/35">
              If it’s urgent, reply to any BioStack email and it will reach the same inbox.
            </p>
          </GlassCard>
        ) : (
          <GlassCard className="p-6 sm:p-8">
            <form onSubmit={(event) => void handleSubmit(event)} className="space-y-4">
              <label className="block">
                <span className="mb-2 block text-sm font-semibold text-white/70">Name</span>
                <input
                  type="text"
                  name="name"
                  autoComplete="name"
                  required
                  value={name}
                  onChange={(event) => setName(event.target.value)}
                  className="min-h-12 w-full rounded-lg border border-white/10 bg-black/25 px-4 text-base text-white outline-none transition-colors placeholder:text-white/25 focus:border-emerald-300/50"
                  placeholder="Your name"
                />
              </label>

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

              <label className="block">
                <span className="mb-2 block text-sm font-semibold text-white/70">Category</span>
                <select
                  name="category"
                  value={category}
                  onChange={(event) => setCategory(event.target.value as (typeof CATEGORIES)[number])}
                  className="min-h-12 w-full rounded-lg border border-white/10 bg-black/25 px-4 text-base text-white outline-none transition-colors focus:border-emerald-300/50"
                >
                  {CATEGORIES.map((option) => (
                    <option key={option} value={option}>
                      {option}
                    </option>
                  ))}
                </select>
              </label>

              <label className="block">
                <span className="mb-2 block text-sm font-semibold text-white/70">Message</span>
                <textarea
                  name="message"
                  required
                  rows={6}
                  maxLength={4000}
                  value={message}
                  onChange={(event) => setMessage(event.target.value)}
                  className="w-full rounded-lg border border-white/10 bg-black/25 px-4 py-3 text-base text-white outline-none transition-colors placeholder:text-white/25 focus:border-emerald-300/50"
                  placeholder="How can we help?"
                />
              </label>

              {error && (
                <div role="alert" className="rounded-lg border border-red-300/20 bg-red-500/10 px-4 py-3 text-sm text-red-100/80">
                  {error}
                </div>
              )}

              <button
                type="submit"
                disabled={status === 'submitting'}
                className="min-h-12 w-full rounded-lg bg-emerald-400 px-5 text-sm font-bold text-[#07110c] transition-colors hover:bg-emerald-300 disabled:cursor-not-allowed disabled:opacity-65"
              >
                {status === 'submitting' ? 'Sending…' : 'Send message'}
              </button>
            </form>
          </GlassCard>
        )}
      </main>
    </div>
  );
}
