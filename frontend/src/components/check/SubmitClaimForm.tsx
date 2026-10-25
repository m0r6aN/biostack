'use client';

import { useState } from 'react';

export const SEEN_ON = [
  ['instagram', 'Instagram'], ['tiktok', 'TikTok'], ['youtube', 'YouTube'], ['x', 'X'],
  ['reddit', 'Reddit'], ['forum', 'Forum'], ['friend', 'Friend'], ['other', 'Other'],
] as const;
export const EXPERIENCES = [['', 'Prefer not to say'], ['side-effect', 'A side effect'], ['benefit', 'A benefit'], ['nothing', 'Nothing']] as const;

interface Props { compounds: { slug: string; name: string }[]; initialClaim?: string; initialCompound?: string }

/** "Submit a claim you saw" (03 §7/§9): no URL, creator, photo or dose fields, by design. */
export function SubmitClaimForm({ compounds, initialClaim = '', initialCompound = '' }: Props) {
  const [state, setState] = useState<{ status: 'idle' | 'sending' | 'done' | 'error'; notice?: string | null; safety?: string | null }>({ status: 'idle' });
  const [experience, setExperience] = useState('');

  async function onSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    setState({ status: 'sending' });
    try {
      const res = await fetch('/api/v1/check/submissions', {
        method: 'POST', headers: { 'Content-Type': 'application/json' }, credentials: 'include',
        body: JSON.stringify({
          compoundSlug: form.get('compound'), claim: form.get('claim'), seenOn: form.get('seenOn'),
          experience: form.get('experience') || null, theme: form.get('theme') || null, website: form.get('website') || null,
        }),
      });
      if (!res.ok) throw new Error(String(res.status));
      const body = await res.json();
      setState({ status: 'done', notice: body.notice, safety: body.safetyMessage });
    } catch {
      setState({ status: 'error' });
    }
  }

  if (state.status === 'done') {
    return (
      <div role="status" className="space-y-2 rounded-xl border border-white/10 bg-white/[0.03] p-4">
        {state.safety && <p className="font-semibold text-amber-200">{state.safety}</p>}
        <p className="text-white/80">Thanks. It&rsquo;s in the queue and an editor will check the receipts.</p>
        {state.notice && <p className="text-sm text-white/60">{state.notice}</p>}
      </div>
    );
  }

  return (
    <form onSubmit={onSubmit} className="space-y-4" aria-label="Submit a claim you saw">
      <label className="block text-sm text-white/80">Compound
        <select name="compound" required defaultValue={initialCompound} className="mt-1 block w-full rounded-lg border border-white/15 bg-black/30 p-2 text-white">
          <option value="" disabled>Pick one</option>
          {compounds.map(c => <option key={c.slug} value={c.slug}>{c.name}</option>)}
        </select>
      </label>
      <label className="block text-sm text-white/80">The claim, in your own words
        <textarea name="claim" required minLength={3} maxLength={280} defaultValue={initialClaim} rows={3}
          className="mt-1 block w-full rounded-lg border border-white/15 bg-black/30 p-2 text-white" />
      </label>
      <label className="block text-sm text-white/80">Where you saw it
        <select name="seenOn" required defaultValue="" className="mt-1 block w-full rounded-lg border border-white/15 bg-black/30 p-2 text-white">
          <option value="" disabled>Pick one</option>
          {SEEN_ON.map(([v, l]) => <option key={v} value={v}>{l}</option>)}
        </select>
      </label>
      <label className="block text-sm text-white/80">I experienced (optional)
        <select name="experience" value={experience} onChange={e => setExperience(e.target.value)} className="mt-1 block w-full rounded-lg border border-white/15 bg-black/30 p-2 text-white">
          {EXPERIENCES.map(([v, l]) => <option key={v} value={v}>{l}</option>)}
        </select>
      </label>
      {experience && experience !== 'nothing' && (
        <label className="block text-sm text-white/80">In a few words (optional)
          <input name="theme" maxLength={140} className="mt-1 block w-full rounded-lg border border-white/15 bg-black/30 p-2 text-white" />
        </label>
      )}
      <input name="website" tabIndex={-1} autoComplete="off" aria-hidden="true" className="hidden" />
      <p className="text-xs text-white/50">No links, usernames, sellers or dose numbers: we strip them automatically.</p>
      <button type="submit" disabled={state.status === 'sending'} className="rounded-lg border border-cyan-300/30 px-4 py-2 text-sm text-cyan-100 hover:bg-cyan-400/10 disabled:opacity-60">
        Send it in
      </button>
      {state.status === 'error' && <p role="alert" className="text-sm text-amber-300">Something broke on our side. Try again in a minute.</p>}
    </form>
  );
}
