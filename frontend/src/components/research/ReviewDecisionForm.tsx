'use client';
import { useState } from 'react';
import { useReviewDecision } from '@/lib/research/ReviewDecisionContext';
import { toJson } from '@/lib/research/reviewDecisionBatch';
import { BlockerCard } from './BlockerCard';
import type { PromotionManifestCandidate, ReviewDecisionType } from '@/lib/research/types';

const DECISIONS: { value: ReviewDecisionType; label: string }[] = [
  { value: 'approve-for-promotion', label: 'approve-for-promotion — clear all blockers, mark candidate' },
  { value: 'approve-claims',        label: 'approve-claims — approve specific claims only' },
  { value: 'request-changes',       label: 'request-changes — flag issues, send back for revision' },
  { value: 'reject',                label: 'reject — remove from promotion pipeline' },
];

export function ReviewDecisionForm({ candidate }: { candidate: PromotionManifestCandidate }) {
  const { batch, reviewerId, setReviewerId, addToSession } = useReviewDecision();
  const hardBlockers = candidate.blockers.filter(b => b.startsWith('blocked:'));
  const softBlockers = candidate.blockers.filter(b => b.startsWith('review-required:'));

  const [decision, setDecision] = useState<ReviewDecisionType | ''>('');
  const [notes, setNotes] = useState('');
  const [clearsSoft, setClearsSoft] = useState<boolean | null>(null);
  const [error, setError] = useState('');
  const [submitted, setSubmitted] = useState(false);

  if (hardBlockers.length > 0) {
    return (
      <div className="flex flex-col gap-3">
        <div className="rounded-xl bg-rose-500/10 border border-rose-400/25 px-4 py-3">
          <p className="text-sm font-semibold text-rose-300 mb-3">
            This compound has unresolved hard blockers. Resolve all <code>blocked:</code> items before submitting a review decision.
          </p>
          {hardBlockers.map(b => <BlockerCard key={b} blocker={b} />)}
        </div>
      </div>
    );
  }

  function handleSubmit() {
    setError('');
    if (!decision) { setError('Select a decision.'); return; }
    if (!reviewerId.trim()) { setError('Reviewer ID is required.'); return; }
    const needsClearNote = decision === 'approve-for-promotion' && softBlockers.length > 0;
    if (needsClearNote && !notes.trim()) {
      setError('Notes required — explain why soft blockers are cleared when approving for promotion.');
      return;
    }
    if (needsClearNote && clearsSoft !== true) {
      setError('Set "Clears Soft Blockers" to Yes when approving with remaining soft blockers.');
      return;
    }
    addToSession({
      decisionId: `dec-${Date.now()}`,
      compoundName: candidate.name,
      decision,
      reviewerId,
      reviewedAt: new Date().toISOString(),
      scope: { claimIds: [], qualityFlags: candidate.qualityFlags, reviewCategories: [], promotionBlockers: candidate.blockers },
      clearsSoftPromotionBlockers: clearsSoft ?? false,
      expiresAt: null,
      notes: notes.trim() ? [notes.trim()] : [],
    });
    setSubmitted(true);
  }

  if (submitted) {
    return (
      <div className="rounded-xl bg-emerald-500/10 border border-emerald-400/25 px-4 py-3">
        <p className="text-sm text-emerald-300">Decision added to batch ({batch.decisions.length + 1} total).</p>
        <button onClick={() => { setSubmitted(false); setDecision(''); setNotes(''); setClearsSoft(null); }}
          className="text-[11px] text-white/40 mt-2 hover:text-white/70">Add another decision</button>
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-4">
      {softBlockers.map(b => <BlockerCard key={b} blocker={b} />)}

      <div>
        <p className="text-[9px] font-bold uppercase tracking-widest text-white/30 mb-2">Decision</p>
        <div className="flex flex-col gap-2">
          {DECISIONS.map(d => (
            <label key={d.value} className={`flex items-center gap-2.5 rounded-xl border px-3 py-2 cursor-pointer transition-all ${decision === d.value ? 'border-emerald-400/40 bg-emerald-500/8' : 'border-white/10 hover:border-white/20'}`}>
              <input type="radio" aria-label={d.value} name="decision" value={d.value} checked={decision === d.value} onChange={() => setDecision(d.value)} className="accent-emerald-500" />
              <span className="text-[11px] text-white/80">{d.label}</span>
            </label>
          ))}
        </div>
      </div>

      <div>
        <p className="text-[9px] font-bold uppercase tracking-widest text-white/30 mb-1.5">Reviewer ID</p>
        <input type="text" value={reviewerId} onChange={e => setReviewerId(e.target.value)} placeholder="your-reviewer-id"
          className="w-full bg-white/[0.05] border border-white/15 rounded-xl px-3 py-2 text-[12px] text-white placeholder-white/25 focus:outline-none focus:border-white/30" />
      </div>

      <div>
        <p className="text-[9px] font-bold uppercase tracking-widest text-white/30 mb-1.5">
          Notes {decision === 'approve-for-promotion' && softBlockers.length > 0 && <span className="text-rose-400">*</span>}
        </p>
        <textarea value={notes} onChange={e => setNotes(e.target.value)} rows={3} placeholder="Add notes for this decision..."
          className="w-full bg-white/[0.05] border border-white/15 rounded-xl px-3 py-2 text-[12px] text-white placeholder-white/25 focus:outline-none focus:border-white/30 resize-none" />
      </div>

      {softBlockers.length > 0 && (
        <div>
          <p className="text-[9px] font-bold uppercase tracking-widest text-white/30 mb-2">
            Clears Soft Promotion Blockers {decision === 'approve-for-promotion' && <span className="text-rose-400">*</span>}
          </p>
          <div className="flex gap-3">
            {([true, false] as const).map(v => (
              <label key={String(v)} className={`flex items-center gap-2 rounded-xl border px-3 py-2 cursor-pointer transition-all text-[11px] ${clearsSoft === v ? 'border-emerald-400/40 bg-emerald-500/8 text-emerald-300' : 'border-white/10 text-white/50 hover:border-white/20'}`}>
                <input type="radio" name="clearsSoft" checked={clearsSoft === v} onChange={() => setClearsSoft(v)} className="accent-emerald-500" />
                {v ? 'Yes' : 'No'}
              </label>
            ))}
          </div>
        </div>
      )}

      {error && <p className="text-[11px] text-rose-300 bg-rose-500/10 border border-rose-400/20 rounded-lg px-3 py-2">{error}</p>}

      <button onClick={handleSubmit} className="bg-emerald-600 hover:bg-emerald-500 text-white text-[12px] font-semibold py-2.5 rounded-xl transition-colors">
        Add to Decision Batch
      </button>

      <div className="text-center text-[10px] text-white/30">
        {batch.decisions.length} decision(s) in batch ·{' '}
        <button onClick={() => {
          const blob = new Blob([toJson(batch)], { type: 'application/json' });
          const a = document.createElement('a');
          a.href = URL.createObjectURL(blob);
          a.download = 'review-decision-batch.json';
          a.click();
        }} className="hover:text-white/60 underline">Export batch as JSON</button>
      </div>
    </div>
  );
}
