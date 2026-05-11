'use client';
import { useReviewDecision } from '@/lib/research/ReviewDecisionContext';
import { toJson } from '@/lib/research/reviewDecisionBatch';
import type { EvidencePacket, PromotionManifestCandidate, ResearchReviewQueueItem, ResearchSummaryCompound, ReviewDecisionType, ReviewResolutionPlanItem } from '@/lib/research/types';
import { useState } from 'react';
import { BlockerCard } from './BlockerCard';

const DECISIONS: { value: ReviewDecisionType; label: string; helper: string }[] = [
  { value: 'approve-claims', label: 'Approve selected claims only', helper: 'Records claim IDs as reviewed. Does not approve the draft or clear blockers by itself.' },
  { value: 'resolve-review-items', label: 'Resolve selected pending items', helper: 'Marks specific review queue items as resolved after the source issue has been fixed or verified.' },
  { value: 'request-changes', label: 'Request draft changes', helper: 'Keeps the compound in the pipeline and asks for more evidence, source, or wording work.' },
  { value: 'approve-for-promotion', label: 'Approve draft for promotion', helper: 'Use only when the compiled draft is ready and soft blockers should be cleared.' },
  { value: 'archive-draft', label: 'Archive draft from active review', helper: 'Removes this generated draft from active queues/manifests after the decision batch is applied; raw artifacts remain.' },
  { value: 'reject', label: 'Reject draft from promotion pipeline', helper: 'Use when the draft/packet should not continue toward promotion.' },
];

type AiReviewSuggestion = {
  decision: ReviewDecisionType;
  confidence: 'low' | 'moderate' | 'high';
  summary: string;
  rationale: string[];
  claimIdsToApprove: string[];
  reviewQueueItemIdsToResolve: string[];
  clearsSoftPromotionBlockers: boolean;
  draftNotes: string;
  safetyWarnings: string[];
  openQuestions: string[];
  modelUsed?: string;
};

type ProcessBatchResult = {
  savedFilename: string;
  exitCode: number | null;
  stdout?: string;
  stderr?: string;
  error?: string;
};

export function ReviewDecisionForm({ candidate, compound, evidencePacket, planItems = [], reviewQueueItems = [] }: { candidate: PromotionManifestCandidate; compound: ResearchSummaryCompound; evidencePacket: EvidencePacket | null; planItems?: ReviewResolutionPlanItem[]; reviewQueueItems?: ResearchReviewQueueItem[] }) {
  const { batch, reviewerId, setReviewerId, addToSession, resetSession } = useReviewDecision();
  const hardBlockers = candidate.blockers.filter(b => b.startsWith('blocked:'));
  const softBlockers = candidate.blockers.filter(b => b.startsWith('review-required:'));
  const claims = evidencePacket?.claims ?? [];

  const [decision, setDecision] = useState<ReviewDecisionType | ''>('');
  const [notes, setNotes] = useState('');
  const [clearsSoft, setClearsSoft] = useState<boolean | null>(null);
  const [selectedClaimIds, setSelectedClaimIds] = useState<Set<string>>(new Set());
  const [selectedReviewQueueItemIds, setSelectedReviewQueueItemIds] = useState<Set<string>>(new Set());
  const [error, setError] = useState('');
  const [submitted, setSubmitted] = useState(false);
  const [submittedCount, setSubmittedCount] = useState(0);
  const [aiLoading, setAiLoading] = useState(false);
  const [aiError, setAiError] = useState('');
  const [aiSuggestion, setAiSuggestion] = useState<AiReviewSuggestion | null>(null);
  const [processing, setProcessing] = useState(false);
  const [processError, setProcessError] = useState('');
  const [processResult, setProcessResult] = useState<ProcessBatchResult | null>(null);

  function toggleClaim(claimId: string) {
    setSelectedClaimIds(prev => {
      const next = new Set(prev);
      if (next.has(claimId)) {
        next.delete(claimId);
      } else {
        next.add(claimId);
      }
      return next;
    });
  }

  function toggleReviewQueueItem(itemId: string) {
    setSelectedReviewQueueItemIds(prev => {
      const next = new Set(prev);
      if (next.has(itemId)) {
        next.delete(itemId);
      } else {
        next.add(itemId);
      }
      return next;
    });
  }

  async function requestAiSuggestion() {
    setAiLoading(true);
    setAiError('');
    setAiSuggestion(null);
    try {
      const response = await fetch('/api/research/suggest', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ compound, candidate, evidencePacket, planItems, reviewQueueItems }),
      });
      const payload = await response.json().catch(() => null) as { suggestion?: AiReviewSuggestion; modelUsed?: string; error?: string } | null;
      if (!response.ok) throw new Error(payload?.error ?? 'AI Suggest failed.');
      if (!payload?.suggestion) throw new Error('AI Suggest returned no recommendation.');
      setAiSuggestion({ ...payload.suggestion, modelUsed: payload.modelUsed });
    } catch (err) {
      setAiError(err instanceof Error ? err.message : 'AI Suggest failed.');
    } finally {
      setAiLoading(false);
    }
  }

  function applyAiSuggestion() {
    if (!aiSuggestion) return;
    const validClaimIds = new Set(claims.map(claim => claim.claimId));
    const suggestedClaimIds = aiSuggestion.claimIdsToApprove.filter(claimId => validClaimIds.has(claimId));
    const validQueueIds = new Set(reviewQueueItems.map(item => item.itemId));
    const suggestedQueueIds = (aiSuggestion.reviewQueueItemIdsToResolve ?? []).filter(itemId => validQueueIds.has(itemId));
    setDecision(aiSuggestion.decision);
    setClearsSoft(aiSuggestion.clearsSoftPromotionBlockers);
    if (suggestedClaimIds.length > 0) setSelectedClaimIds(new Set(suggestedClaimIds));
    if (suggestedQueueIds.length > 0) setSelectedReviewQueueItemIds(new Set(suggestedQueueIds));
    if (aiSuggestion.draftNotes.trim()) {
      setNotes(prev => prev.trim() ? `${prev.trim()}\n\nAI suggestion draft:\n${aiSuggestion.draftNotes.trim()}` : aiSuggestion.draftNotes.trim());
    }
  }

  function handleSubmit() {
    setError('');
    if (!decision) { setError('Select a decision.'); return; }
    if (!reviewerId.trim()) { setError('Reviewer ID is required.'); return; }
    if (decision === 'approve-claims' && selectedClaimIds.size === 0) {
      setError('Select at least one evidence claim to approve.');
      return;
    }
    if (decision === 'resolve-review-items' && selectedReviewQueueItemIds.size === 0) {
      setError('Select at least one pending review item to resolve.');
      return;
    }
    if (decision === 'archive-draft' && !notes.trim()) {
      setError('Notes required — explain why this draft is being archived from active review.');
      return;
    }
    if (decision === 'approve-for-promotion' && hardBlockers.length > 0) {
      setError('Hard blockers cannot be cleared by review. Request changes or resolve the hard blockers first.');
      return;
    }
    const needsClearNote = decision === 'approve-for-promotion' && softBlockers.length > 0;
    if (needsClearNote && !notes.trim()) {
      setError('Notes required — explain why soft blockers are cleared when approving for promotion.');
      return;
    }
    if (needsClearNote && clearsSoft !== true) {
      setError('Set "Clears Soft Blockers" to Yes when approving with remaining soft blockers.');
      return;
    }
    const nextCount = batch.decisions.length + 1;
    addToSession({
      decisionId: `dec-${Date.now()}`,
      compoundName: candidate.name,
      decision,
      reviewerId,
      reviewedAt: new Date().toISOString(),
      scope: {
        claimIds: decision === 'approve-claims' ? Array.from(selectedClaimIds) : [],
        reviewQueueItemIds: decision === 'resolve-review-items' ? Array.from(selectedReviewQueueItemIds) : [],
        qualityFlags: candidate.qualityFlags,
        reviewCategories: [],
        promotionBlockers: candidate.blockers,
      },
      clearsSoftPromotionBlockers: decision === 'approve-for-promotion' ? clearsSoft ?? false : false,
      expiresAt: null,
      notes: notes.trim() ? [notes.trim()] : [],
    });
    setSubmittedCount(nextCount);
    setSubmitted(true);
  }

  function exportBatch() {
    const blob = new Blob([toJson(batch)], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'review-decision-batch.json';
    a.click();
    URL.revokeObjectURL(url);
  }

  async function processBatch() {
    setProcessError('');
    setProcessResult(null);
    if (batch.decisions.length === 0) {
      setProcessError('Add at least one decision before processing the batch.');
      return;
    }

    setProcessing(true);
    try {
      const response = await fetch('/api/research/process-decisions', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: toJson(batch),
      });
      const payload = await response.json().catch(() => null) as ProcessBatchResult | null;
      if (!response.ok) throw new Error(payload?.error ?? `Process batch failed (${response.status}).`);
      if (!payload) throw new Error('Process batch returned no result.');
      setProcessResult(payload);
      resetSession();
      if (process.env.NODE_ENV !== 'test') {
        window.setTimeout(() => window.location.reload(), 900);
      }
    } catch (err) {
      setProcessError(err instanceof Error ? err.message : 'Process batch failed.');
    } finally {
      setProcessing(false);
    }
  }

  if (submitted) {
    return (
      <div className="flex flex-col gap-4">
        <div className="rounded-xl bg-emerald-500/10 border border-emerald-400/25 px-4 py-3">
          <p className="text-sm text-emerald-300">Decision added to batch ({submittedCount} total).</p>
          <button onClick={() => { setSubmitted(false); setDecision(''); setNotes(''); setClearsSoft(null); setSelectedClaimIds(new Set()); setSelectedReviewQueueItemIds(new Set()); }}
            className="text-[11px] text-white/40 mt-2 hover:text-white/70">Add another decision</button>
        </div>
        <BatchActions
          decisionCount={batch.decisions.length}
          onExport={exportBatch}
          onProcess={processBatch}
          processing={processing}
          processError={processError}
          processResult={processResult}
        />
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-4">
      <div className="rounded-xl border border-blue-400/20 bg-blue-500/10 px-4 py-3">
        <p className="text-[11px] font-semibold uppercase tracking-widest text-blue-200/80">What this form records</p>
        <p className="mt-1 text-[12px] leading-5 text-blue-100/70">
          Claim approval, pending-item resolution, draft archival, draft approval, and blocker clearance are separate. Approving claims records selected claim IDs only; it does not promote the compound.
        </p>
      </div>

      <div className="rounded-xl border border-fuchsia-300/20 bg-fuchsia-500/10 px-4 py-3">
        <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
          <div>
            <p className="text-[11px] font-semibold uppercase tracking-widest text-fuchsia-200/80">AI Review Assistant</p>
            <p className="mt-1 text-[12px] leading-5 text-fuchsia-100/70">
              Generates an advisory recommendation from the loaded evidence packet, blockers, and remediation tasks. It never submits decisions for you.
            </p>
          </div>
          <button
            onClick={requestAiSuggestion}
            disabled={aiLoading}
            className="rounded-xl border border-fuchsia-200/25 bg-fuchsia-400/10 px-3 py-2 text-[11px] font-semibold text-fuchsia-100 transition-colors hover:bg-fuchsia-400/20 disabled:cursor-not-allowed disabled:opacity-50"
          >
            {aiLoading ? 'Thinking…' : 'AI Suggest'}
          </button>
        </div>
        {aiError && <p className="mt-3 rounded-lg border border-rose-400/20 bg-rose-500/10 px-3 py-2 text-[11px] text-rose-200">{aiError}</p>}
        {aiSuggestion && <AiSuggestionCard suggestion={aiSuggestion} onApply={applyAiSuggestion} />}
      </div>

      {hardBlockers.length > 0 && (
        <div className="rounded-xl bg-rose-500/10 border border-rose-400/25 px-4 py-3">
          <p className="text-[11px] font-semibold uppercase tracking-widest text-rose-300 mb-2">Hard blockers — cannot be cleared here</p>
          {hardBlockers.map(b => <BlockerCard key={b} blocker={b} />)}
        </div>
      )}

      {softBlockers.length > 0 && (
        <div>
          <p className="text-[9px] font-bold uppercase tracking-widest text-white/30 mb-2">Soft promotion blockers currently active</p>
          <div className="flex flex-col gap-2">{softBlockers.map(b => <BlockerCard key={b} blocker={b} />)}</div>
        </div>
      )}

      {compound.reviewReasons.length > 0 && (
        <div className="rounded-xl border border-white/10 bg-white/[0.03] px-3 py-2.5">
          <p className="text-[9px] font-bold uppercase tracking-widest text-white/30 mb-2">Review findings still attached to draft</p>
          <div className="flex flex-col gap-1.5">
            {compound.reviewReasons.map(reason => <p key={reason} className="text-[11px] leading-5 text-white/55">• {reason}</p>)}
          </div>
        </div>
      )}

      <div>
        <p className="text-[9px] font-bold uppercase tracking-widest text-white/30 mb-2">Decision scope</p>
        <div className="flex flex-col gap-2">
          {DECISIONS.map(d => (
            <label key={d.value} className={`flex items-center gap-2.5 rounded-xl border px-3 py-2 cursor-pointer transition-all ${decision === d.value ? 'border-emerald-400/40 bg-emerald-500/8' : 'border-white/10 hover:border-white/20'}`}>
              <input type="radio" aria-label={d.value} name="decision" value={d.value} checked={decision === d.value} onChange={() => setDecision(d.value)} className="accent-emerald-500" />
              <span className="flex flex-col gap-0.5">
                <span className="text-[11px] text-white/85">{d.label}</span>
                <span className="text-[10px] text-white/35">{d.helper}</span>
              </span>
            </label>
          ))}
        </div>
      </div>

      {decision === 'approve-claims' && (
        <div>
          <p className="text-[9px] font-bold uppercase tracking-widest text-white/30 mb-2">
            Evidence claims to approve <span className="text-rose-400">*</span>
          </p>
          {claims.length === 0 ? (
            <p className="text-[11px] text-white/30">No per-compound evidence packet is loaded, so claim-level approval is unavailable.</p>
          ) : (
            <div className="flex flex-col gap-2">
              {claims.map(claim => (
                <label key={claim.claimId} className={`flex items-start gap-2.5 rounded-xl border px-3 py-2.5 cursor-pointer transition-all ${selectedClaimIds.has(claim.claimId) ? 'border-emerald-400/40 bg-emerald-500/8' : 'border-white/10 hover:border-white/20'}`}>
                  <input
                    type="checkbox"
                    checked={selectedClaimIds.has(claim.claimId)}
                    onChange={() => toggleClaim(claim.claimId)}
                    className="mt-0.5 accent-emerald-500 shrink-0"
                  />
                  <span className="flex flex-col gap-1 text-[11px] text-white/80">
                    <span className="font-mono text-[10px] text-emerald-200/80">{claim.claimId}</span>
                    <span>{claim.statement}</span>
                    <span className="text-[10px] text-white/35">Approve only this source-scoped claim; draft promotion remains separate.</span>
                  </span>
                </label>
              ))}
            </div>
          )}
        </div>
      )}

      {decision === 'resolve-review-items' && (
        <div>
          <p className="text-[9px] font-bold uppercase tracking-widest text-white/30 mb-2">
            Pending review items to resolve <span className="text-rose-400">*</span>
          </p>
          <p className="mb-2 text-[11px] leading-5 text-white/40">
            Select only items that are genuinely fixed or no longer pending. They disappear after this batch is applied and artifacts regenerate.
          </p>
          {reviewQueueItems.length === 0 ? (
            <p className="text-[11px] text-white/30">No active review queue items are loaded for this compound.</p>
          ) : (
            <div className="flex flex-col gap-2">
              {reviewQueueItems.map(item => (
                <label key={item.itemId} className={`flex items-start gap-2.5 rounded-xl border px-3 py-2.5 cursor-pointer transition-all ${selectedReviewQueueItemIds.has(item.itemId) ? 'border-emerald-400/40 bg-emerald-500/8' : 'border-white/10 hover:border-white/20'}`}>
                  <input
                    type="checkbox"
                    checked={selectedReviewQueueItemIds.has(item.itemId)}
                    onChange={() => toggleReviewQueueItem(item.itemId)}
                    className="mt-0.5 accent-emerald-500 shrink-0"
                  />
                  <span className="flex flex-col gap-1 text-[11px] text-white/80">
                    <span className="font-mono text-[10px] text-emerald-200/80">{item.itemId}</span>
                    <span>{item.reason}</span>
                    <span className="text-[10px] text-white/35">Severity: {item.severity} · References: {item.references.length > 0 ? item.references.join(', ') : 'none'}</span>
                  </span>
                </label>
              ))}
            </div>
          )}
        </div>
      )}

      {decision === 'archive-draft' && (
        <div className="rounded-xl border border-amber-300/20 bg-amber-500/10 px-3 py-2.5">
          <p className="text-[11px] font-semibold uppercase tracking-widest text-amber-200/80">Archive behavior</p>
          <p className="mt-1 text-[11px] leading-5 text-amber-100/70">
            This removes the draft from active summary, review queues, promotion manifest, and promotion export after the decision batch is applied. Raw generated artifacts remain for audit.
          </p>
        </div>
      )}

      <div>
        <p className="text-[9px] font-bold uppercase tracking-widest text-white/30 mb-1.5">Reviewer ID</p>
        <input type="text" value={reviewerId} onChange={e => setReviewerId(e.target.value)} placeholder="your-reviewer-id"
          className="w-full bg-white/[0.05] border border-white/15 rounded-xl px-3 py-2 text-[12px] text-white placeholder-white/25 focus:outline-none focus:border-white/30" />
      </div>

      <div>
        <p className="text-[9px] font-bold uppercase tracking-widest text-white/30 mb-1.5">
          Notes {((decision === 'approve-for-promotion' && softBlockers.length > 0) || decision === 'archive-draft') && <span className="text-rose-400">*</span>}
        </p>
        <textarea value={notes} onChange={e => setNotes(e.target.value)} rows={3} placeholder="Add notes for this decision..."
          className="w-full bg-white/[0.05] border border-white/15 rounded-xl px-3 py-2 text-[12px] text-white placeholder-white/25 focus:outline-none focus:border-white/30 resize-none" />
      </div>

      {softBlockers.length > 0 && (
        <div>
          <p className="text-[9px] font-bold uppercase tracking-widest text-white/30 mb-2">
            Clear soft promotion blockers? {decision === 'approve-for-promotion' && <span className="text-rose-400">*</span>}
          </p>
          <p className="mb-2 text-[11px] leading-5 text-white/40">
            Select Yes only for draft promotion approval. Claim approval should normally leave this set to No.
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

      <BatchActions
        decisionCount={batch.decisions.length}
        onExport={exportBatch}
        onProcess={processBatch}
        processing={processing}
        processError={processError}
        processResult={processResult}
      />
    </div>
  );
}

function BatchActions({ decisionCount, onExport, onProcess, processing, processError, processResult }: { decisionCount: number; onExport: () => void; onProcess: () => void; processing: boolean; processError: string; processResult: ProcessBatchResult | null }) {
  return (
    <div className="rounded-xl border border-white/10 bg-white/[0.03] px-3 py-3 text-center">
      <p className="text-[10px] text-white/35">{decisionCount} decision(s) in batch</p>
      <div className="mt-2 flex flex-col justify-center gap-2 sm:flex-row">
        <button onClick={onExport} disabled={decisionCount === 0}
          className="rounded-lg border border-white/15 px-3 py-2 text-[11px] font-semibold text-white/55 transition-colors hover:border-white/25 hover:text-white/75 disabled:cursor-not-allowed disabled:opacity-40">
          Export batch as JSON
        </button>
        <button onClick={onProcess} disabled={processing || decisionCount === 0}
          className="rounded-lg bg-emerald-600 px-3 py-2 text-[11px] font-semibold text-white transition-colors hover:bg-emerald-500 disabled:cursor-not-allowed disabled:opacity-50">
          {processing ? 'Saving & processing…' : 'Save & Process Batch'}
        </button>
      </div>
      <p className="mt-2 text-[10px] leading-4 text-white/30">
        Requires local <code className="text-white/45">RESEARCH_AUTOMATION_ENABLED=true</code>. On success, artifacts regenerate and the page refreshes.
      </p>
      {processError && <p className="mt-2 rounded-lg border border-rose-400/20 bg-rose-500/10 px-3 py-2 text-[11px] text-rose-200">{processError}</p>}
      {processResult && (
        <div className="mt-2 rounded-lg border border-emerald-400/20 bg-emerald-500/10 px-3 py-2 text-left">
          <p className="text-[11px] font-semibold text-emerald-200">Processed {processResult.savedFilename}</p>
          <p className="mt-1 text-[10px] text-emerald-100/55">Worker exit code: {processResult.exitCode ?? 'unknown'} · Refreshing artifacts…</p>
        </div>
      )}
    </div>
  );
}

function AiSuggestionCard({ suggestion, onApply }: { suggestion: AiReviewSuggestion; onApply: () => void }) {
  const decisionLabel = DECISIONS.find(decision => decision.value === suggestion.decision)?.label ?? suggestion.decision;

  return (
    <div className="mt-3 rounded-xl border border-white/10 bg-black/15 p-3">
      <div className="mb-2 flex flex-wrap items-center gap-2">
        <span className="rounded-full border border-fuchsia-200/25 bg-fuchsia-400/10 px-2 py-1 text-[10px] text-fuchsia-100">{decisionLabel}</span>
        <span className="text-[10px] text-white/40">AI confidence: {suggestion.confidence}</span>
        {suggestion.modelUsed && <span className="text-[10px] text-white/25">Model: {suggestion.modelUsed}</span>}
      </div>
      <p className="text-[12px] leading-5 text-white/75">{suggestion.summary}</p>

      <div className="mt-3 grid gap-3 lg:grid-cols-2">
        <SuggestionList title="Rationale" items={suggestion.rationale} />
        <SuggestionList title="Safety / review cautions" items={suggestion.safetyWarnings} />
        <SuggestionList title="Suggested claim IDs" items={suggestion.claimIdsToApprove} empty="No claim-level approvals suggested." />
        <SuggestionList title="Pending items to resolve" items={suggestion.reviewQueueItemIdsToResolve} empty="No queue item resolutions suggested." />
        <SuggestionList title="Open questions" items={suggestion.openQuestions} empty="No open questions returned." />
      </div>

      {suggestion.draftNotes && (
        <div className="mt-3 rounded-lg border border-white/10 bg-white/[0.03] px-3 py-2">
          <p className="mb-1 text-[9px] font-bold uppercase tracking-widest text-white/30">Draft reviewer notes</p>
          <p className="whitespace-pre-wrap text-[11px] leading-5 text-white/60">{suggestion.draftNotes}</p>
        </div>
      )}

      <button onClick={onApply} className="mt-3 rounded-lg bg-fuchsia-600 px-3 py-2 text-[11px] font-semibold text-white transition-colors hover:bg-fuchsia-500">
        Apply suggestion to form
      </button>
    </div>
  );
}

function SuggestionList({ title, items, empty = 'None returned.' }: { title: string; items: string[]; empty?: string }) {
  return (
    <div>
      <p className="mb-1 text-[9px] font-bold uppercase tracking-widest text-white/30">{title}</p>
      {items.length === 0 ? (
        <p className="text-[11px] text-white/30">{empty}</p>
      ) : (
        <ul className="space-y-1 text-[11px] leading-5 text-white/55">
          {items.map(item => <li key={item}>• {item}</li>)}
        </ul>
      )}
    </div>
  );
}
