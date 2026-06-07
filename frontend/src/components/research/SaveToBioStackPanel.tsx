'use client';

import { GlassCard } from '@/components/ui/GlassCard';
import { Button } from '@/components/ui/Button';
import { getApiBaseUrl } from '@/lib/apiBase';
import type { PromotionPreview, StagedTranscriptCandidateReview } from '@/lib/types';
import { useState } from 'react';

// ── Types ─────────────────────────────────────────────────────────────────

type Phase =
  | 'idle'
  | 'loading'
  | 'preview_passed'
  | 'preview_blocked'
  | 'already_promoted'
  | 'executing'
  | 'success'
  | 'error';

export interface SaveToBioStackPanelProps {
  artifactId: string;
  review: StagedTranscriptCandidateReview;
  authHeaders?: Record<string, string>;
}

// ── Constants ──────────────────────────────────────────────────────────────

const SAFETY_NOTE =
  'BioStack stores educational reference knowledge only. This is not medical advice.';

// ── Sub-components ────────────────────────────────────────────────────────

function PreviewDetails({ preview }: { preview: PromotionPreview }) {
  return (
    <div className="space-y-1.5 text-xs">
      <Row label="Review state" value={preview.reviewState} />
      <Row
        label="Target"
        value={
          preview.targetCanonicalName
            ? <span className="text-white/80 font-medium">{preview.targetCanonicalName}</span>
            : <span className="text-amber-400">Not assigned</span>
        }
      />
      {preview.resolvedTargetKnowledgeEntryId && (
        <Row
          label="KE ID"
          value={<code className="text-white/60 text-[10px]">{preview.resolvedTargetKnowledgeEntryId}</code>}
        />
      )}
      <Row
        label="Evidence gate"
        value={
          <span className={preview.evidenceGate.passed ? 'text-emerald-400' : 'text-rose-400'}>
            {preview.evidenceGate.passed ? 'Passed' : 'Failed'}
          </span>
        }
      />
      {!preview.evidenceGate.passed && preview.evidenceGate.failureReasons.length > 0 && (
        <div className="rounded-lg bg-rose-500/10 border border-rose-400/15 px-2 py-1.5 space-y-0.5">
          {preview.evidenceGate.failureReasons.map(r => (
            <p key={r} className="text-[10px] text-rose-300">{r}</p>
          ))}
        </div>
      )}
      {preview.evidenceGate.tier && (
        <Row label="Evidence tier" value={preview.evidenceGate.tier} />
      )}
      <Row label="Citations" value={String(preview.evidenceGate.citationCount)} />
      <Row label="WouldWrite" value={<span className="text-white/30">{String(preview.wouldWrite)}</span>} />
    </div>
  );
}

function Row({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div className="flex justify-between items-start gap-2">
      <span className="text-white/40 shrink-0">{label}</span>
      <span className="text-right text-white/70">{value}</span>
    </div>
  );
}

function SafetyNote() {
  return <p className="text-[10px] text-white/25 italic">{SAFETY_NOTE}</p>;
}

// ── Main component ────────────────────────────────────────────────────────

export function SaveToBioStackPanel({
  artifactId,
  review,
  authHeaders = {},
}: SaveToBioStackPanelProps) {
  const [phase, setPhase] = useState<Phase>(() =>
    review.promotedKnowledgeEntryId ? 'already_promoted' : 'idle'
  );
  const [preview, setPreview] = useState<PromotionPreview | null>(null);
  const [successRecord, setSuccessRecord] = useState<StagedTranscriptCandidateReview | null>(null);
  const [errorMessage, setErrorMessage] = useState('');

  const isEligible =
    review.reviewState === 'review_approved_for_promotion' &&
    !review.promotedKnowledgeEntryId;

  const jsonHeaders = { 'Content-Type': 'application/json', ...authHeaders };
  const apiBase = getApiBaseUrl();

  async function handlePreview() {
    setPhase('loading');
    setErrorMessage('');
    try {
      const res = await fetch(
        `${apiBase}/api/v1/admin/staged-transcript-candidate-reviews/${encodeURIComponent(artifactId)}/promotion-preview`,
        { method: 'POST', headers: jsonHeaders }
      );
      if (!res.ok) {
        const body = await res.text().catch(() => '');
        setErrorMessage(`Preview failed: ${res.status}${body ? ` — ${body}` : ''}`);
        setPhase('error');
        return;
      }
      const data: PromotionPreview = await res.json();
      setPreview(data);
      if (data.alreadyPromoted) {
        setPhase('already_promoted');
      } else if (data.canPromote) {
        setPhase('preview_passed');
      } else {
        setPhase('preview_blocked');
      }
    } catch (e) {
      setErrorMessage(e instanceof Error ? e.message : 'Preview failed');
      setPhase('error');
    }
  }

  async function handleExecute() {
    setPhase('executing');
    setErrorMessage('');
    try {
      const res = await fetch(
        `${apiBase}/api/v1/admin/staged-transcript-candidate-reviews/${encodeURIComponent(artifactId)}/execute-promotion`,
        { method: 'POST', headers: jsonHeaders }
      );
      if (!res.ok) {
        const body = await res.text().catch(() => '');
        setErrorMessage(`Save failed: ${res.status}${body ? ` — ${body}` : ''}`);
        setPhase('error');
        return;
      }
      const record: StagedTranscriptCandidateReview = await res.json();
      setSuccessRecord(record);
      setPhase('success');
    } catch (e) {
      setErrorMessage(e instanceof Error ? e.message : 'Save failed');
      setPhase('error');
    }
  }

  // ── Render ───────────────────────────────────────────────────────────────

  return (
    <GlassCard className="p-4 space-y-3">
      {/* Already promoted — initial or detected via preview */}
      {phase === 'already_promoted' && (
        <div className="space-y-1">
          <p className="text-xs font-bold text-emerald-400 uppercase tracking-widest">
            Already saved to BioStack
          </p>
          <p className="text-xs text-white/50">
            Knowledge entry:{' '}
            <code className="text-white/70">
              {preview?.promotedKnowledgeEntryId ?? review.promotedKnowledgeEntryId}
            </code>
          </p>
          {(review.promotedAtUtc ?? preview?.promotedKnowledgeEntryId) && (
            <p className="text-xs text-white/40">
              {review.promotedAtUtc ? `Saved: ${review.promotedAtUtc}` : ''}
            </p>
          )}
          <SafetyNote />
        </div>
      )}

      {/* Idle + eligible — show Save button */}
      {phase === 'idle' && isEligible && (
        <div className="space-y-2">
          <Button
            variant="primary"
            onClick={handlePreview}
            className="bg-emerald-600 hover:bg-emerald-500 text-sm"
          >
            Save to BioStack
          </Button>
          <SafetyNote />
        </div>
      )}

      {/* Idle + not eligible — show reason */}
      {phase === 'idle' && !isEligible && (
        <p className="text-xs text-white/30 italic">
          {review.reviewState === 'pending_review'
            ? 'Pending review — not yet eligible to save.'
            : 'Not eligible to save to BioStack in current state.'}
        </p>
      )}

      {/* Preview loading */}
      {phase === 'loading' && (
        <p className="text-sm text-white/50 animate-pulse">
          Checking promotion readiness…
        </p>
      )}

      {/* Preview passed */}
      {phase === 'preview_passed' && preview && (
        <div className="space-y-3">
          <div className="flex items-center gap-2">
            <span className="w-2 h-2 rounded-full bg-emerald-500 shrink-0" />
            <p className="text-sm font-semibold text-emerald-400">Ready to save</p>
          </div>
          <PreviewDetails preview={preview} />
          <Button
            variant="primary"
            onClick={handleExecute}
            className="bg-emerald-600 hover:bg-emerald-500 text-sm"
          >
            Execute Save to BioStack
          </Button>
          <SafetyNote />
        </div>
      )}

      {/* Preview blocked */}
      {phase === 'preview_blocked' && preview && (
        <div className="space-y-3">
          <div className="flex items-center gap-2">
            <span className="w-2 h-2 rounded-full bg-rose-500 shrink-0" />
            <p className="text-sm font-semibold text-rose-400">Cannot save yet</p>
          </div>
          <PreviewDetails preview={preview} />
          {preview.blockingReasons.length > 0 && (
            <div className="rounded-xl bg-rose-500/10 border border-rose-400/20 px-3 py-2">
              <p className="text-[10px] font-bold uppercase tracking-widest text-rose-300 mb-1">
                Blocking reasons
              </p>
              {preview.blockingReasons.map(r => (
                <p key={r} className="text-xs text-rose-300">{r}</p>
              ))}
            </div>
          )}
        </div>
      )}

      {/* Executing */}
      {phase === 'executing' && (
        <p className="text-sm text-white/50 animate-pulse">Saving to BioStack…</p>
      )}

      {/* Success */}
      {phase === 'success' && successRecord && (
        <div className="space-y-1">
          <p className="text-xs font-bold text-emerald-400 uppercase tracking-widest">
            Saved to BioStack
          </p>
          {successRecord.promotedKnowledgeEntryId && (
            <p className="text-xs text-white/50">
              Knowledge entry:{' '}
              <code className="text-white/70">{successRecord.promotedKnowledgeEntryId}</code>
            </p>
          )}
          {successRecord.promotedAtUtc && (
            <p className="text-xs text-white/40">Saved: {successRecord.promotedAtUtc}</p>
          )}
          <SafetyNote />
        </div>
      )}

      {/* Error */}
      {phase === 'error' && (
        <div className="rounded-xl bg-rose-500/10 border border-rose-400/20 px-3 py-2 space-y-2">
          <p className="text-xs text-rose-300">{errorMessage}</p>
          <button
            onClick={() => setPhase('idle')}
            className="text-[10px] text-white/40 hover:text-white/60 underline"
          >
            Try again
          </button>
        </div>
      )}
    </GlassCard>
  );
}
