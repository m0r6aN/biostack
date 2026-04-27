'use client';

/**
 * Stack Review Board — cognitive deliberation UI.
 *
 * DOCTRINE:
 *   - The deterministic safety panel ALWAYS renders first.
 *   - The Stack Review Board is commentary ONLY. It cannot override or
 *     suppress any safety finding.
 *   - No artifact here is effect-bearing. The badge is mandatory.
 *
 * Layout:
 *   DeterministicSafetyPanel   ← always top
 *   ─── Stack Review Board ─── ← divider
 *   GoalAlignmentPanel         ← Optimizer perspective
 *   EvidenceGapsPanel          ← Skeptic perspective
 *   ClaimRiskPanel             ← Regulator perspective
 *   PatternRecognitionPanel    ← Historian + BioStack KnownPatterns
 *   ChallengePanel             ← ContradictionReview
 */

import type {
    SrbCognitiveDensityEnvelope,
    SrbDeterministicFinding,
    SrbKnownPattern
} from '@/lib/types';

export interface StackReviewBoardProps {
  /** Deterministic safety findings — rendered FIRST, above all cognitive panels. */
  deterministicFindings: SrbDeterministicFinding[];
  /** BioStack known patterns from the envelope — surfaced in Pattern Recognition. */
  knownPatterns: SrbKnownPattern[];
  /** Cognitive density envelope from the keon.collective orchestrator. */
  review: SrbCognitiveDensityEnvelope | null;
  /** When true, shows a loading skeleton instead of panels. */
  loading?: boolean;
}

export function StackReviewBoard({
  deterministicFindings,
  knownPatterns,
  review,
  loading = false,
}: StackReviewBoardProps) {
  const optimizer = review?.branchPerspectiveReview['Optimizer'] ?? null;
  const skeptic = review?.branchPerspectiveReview['Skeptic'] ?? null;
  const regulator = review?.branchPerspectiveReview['Regulator'] ?? null;
  const historian = review?.branchPerspectiveReview['Historian'] ?? null;

  return (
    <div data-testid="stack-review-board" className="space-y-4">
      {/* ── 1. Deterministic safety panel — ALWAYS FIRST ── */}
      <DeterministicSafetyPanel findings={deterministicFindings} />

      {/* ── Divider ── */}
      <div
        data-testid="srb-divider"
        className="flex items-center gap-3 py-1"
        aria-label="Stack Review Board section — commentary only"
      >
        <div className="h-px flex-1 bg-white/[0.08]" />
        <span className="text-xs font-semibold uppercase tracking-[0.2em] text-white/38">
          Stack Review Board · commentary only
        </span>
        <div className="h-px flex-1 bg-white/[0.08]" />
      </div>

      {/* ── Non-executable badge ── */}
      <NonExecutableBadge />

      <div className="grid gap-4 xl:grid-cols-[1fr_280px]">
        <div className="space-y-4">
          {loading ? (
            <SrbLoadingSkeleton />
          ) : (
            <>
              {/* ── 2. Goal alignment & sequencing (Optimizer) ── */}
              <GoalAlignmentPanel perspective={optimizer} />

              {/* ── 3. Evidence gaps & attribution risk (Skeptic) ── */}
              <EvidenceGapsPanel perspective={skeptic} />

              {/* ── 4. Claim risk & provider review (Regulator) ── */}
              <ClaimRiskPanel perspective={regulator} />

              {/* ── 5. Pattern recognition (Historian + BioStack patterns) ── */}
              <PatternRecognitionPanel
                perspective={historian}
                knownPatterns={knownPatterns}
              />

              {/* ── 6. Challenge This Stack ── */}
              <ChallengePanel contradiction={review?.contradictionReview ?? null} />
            </>
          )}
        </div>

        {/* ── Sidebar: Confidence Profile + Reasoning Graph ── */}
        <aside className="space-y-4 xl:sticky xl:top-24 xl:self-start">
          {review && (
            <ConfidenceProfileSidebar
              profile={review.confidenceProfile}
              graphRef={review.reasoningGraphRef}
            />
          )}
        </aside>
      </div>
    </div>
  );
}

// ─────────────────────────────────────────────────────────────────────────────
// Sub-components
// ─────────────────────────────────────────────────────────────────────────────

function NonExecutableBadge() {
  return (
    <div
      data-testid="non-executable-badge"
      className="inline-flex items-center gap-2 rounded-full border border-amber-300/25 bg-amber-400/[0.08] px-3 py-1"
    >
      <span className="h-1.5 w-1.5 rounded-full bg-amber-300/70" aria-hidden="true" />
      <span className="text-xs font-semibold uppercase tracking-[0.18em] text-amber-100/70">
        Non-executable · deliberation commentary only
      </span>
    </div>
  );
}

function DeterministicSafetyPanel({ findings }: { findings: SrbDeterministicFinding[] }) {
  return (
    <section
      data-testid="deterministic-safety-panel"
      className="rounded-lg border border-white/[0.08] bg-[#121923]/95 p-4"
    >
      <div className="flex items-center gap-3">
        <p className="text-xs font-semibold uppercase tracking-[0.2em] text-white/55">
          Safety Panel
        </p>
        <span className="rounded-full border border-emerald-300/20 bg-emerald-400/10 px-2 py-0.5 text-xs font-semibold text-emerald-100/80">
          Deterministic · authoritative
        </span>
      </div>
      {findings.length === 0 ? (
        <p className="mt-3 text-sm text-white/45">No deterministic findings for this stack.</p>
      ) : (
        <ul className="mt-3 space-y-2">
          {findings.map((f) => (
            <li key={f.findingId} className="rounded-lg border border-white/10 bg-black/20 p-3">
              <div className="flex items-center gap-2">
                <span className="text-xs font-semibold text-white/50">{f.code}</span>
                <RiskBadge contribution={f.riskScoreContribution} />
              </div>
              <p className="mt-1 text-sm leading-6 text-white/78">{f.narrative}</p>
              {f.compoundSlugs.length > 0 && (
                <p className="mt-1 text-xs text-white/40">
                  Compounds: {f.compoundSlugs.join(', ')}
                </p>
              )}
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}

function RiskBadge({ contribution }: { contribution: number }) {
  const high = contribution >= 0.15;
  const med = contribution >= 0.05;
  const cls = high
    ? 'border-red-300/25 bg-red-400/10 text-red-100/80'
    : med
      ? 'border-amber-300/25 bg-amber-400/10 text-amber-100/80'
      : 'border-white/10 bg-white/[0.04] text-white/50';
  return (
    <span className={`rounded-full border px-2 py-0.5 text-xs font-semibold ${cls}`}>
      risk +{Math.round(contribution * 100)}%
    </span>
  );
}

function SrbPanel({
  title,
  badge,
  summary,
  children,
}: {
  title: string;
  badge?: string;
  summary?: string;
  children: React.ReactNode;
}) {
  return (
    <section className="rounded-lg border border-white/[0.08] bg-white/[0.03] p-4">
      <div className="flex flex-wrap items-center gap-2">
        <h3 className="text-base font-semibold text-white">{title}</h3>
        {badge && (
          <span className="rounded-full border border-white/10 px-2 py-0.5 text-xs font-semibold uppercase tracking-[0.14em] text-white/42">
            {badge}
          </span>
        )}
      </div>
      {summary && <p className="mt-1 text-xs leading-5 text-white/48">{summary}</p>}
      <div className="mt-3">{children}</div>
    </section>
  );
}

function FindingList({ findings }: { findings: SrbPerspectiveFinding[] }) {
  if (findings.length === 0) {
    return <p className="text-sm text-white/45">No findings for this perspective.</p>;
  }
  return (
    <ul className="space-y-2">
      {findings.map((f) => (
        <li key={f.findingId} className="flex gap-3 text-sm leading-6">
          <SeverityDot severity={f.severity} />
          <div>
            <span className="font-semibold text-white/80">{f.category}: </span>
            <span className="text-white/65">{f.narrative}</span>
          </div>
        </li>
      ))}
    </ul>
  );
}

function SeverityDot({ severity }: { severity: string }) {
  const cls =
    severity === 'Critical'
      ? 'bg-red-400'
      : severity === 'Warning'
        ? 'bg-amber-400'
        : 'bg-emerald-400/70';
  return <span className={`mt-2 h-1.5 w-1.5 shrink-0 rounded-full ${cls}`} aria-hidden="true" />;
}

function GoalAlignmentPanel({ perspective }: { perspective: SrbPerspectiveReview | null }) {
  return (
    <SrbPanel title="Goal Alignment & Sequencing" badge="Optimizer" summary={perspective?.summary}>
      <FindingList findings={perspective?.findings ?? []} />
    </SrbPanel>
  );
}

function EvidenceGapsPanel({ perspective }: { perspective: SrbPerspectiveReview | null }) {
  return (
    <SrbPanel title="Evidence Gaps & Attribution Risk" badge="Skeptic" summary={perspective?.summary}>
      <FindingList findings={perspective?.findings ?? []} />
    </SrbPanel>
  );
}

function ClaimRiskPanel({ perspective }: { perspective: SrbPerspectiveReview | null }) {
  return (
    <SrbPanel title="Claim Risk & Provider Review" badge="Regulator" summary={perspective?.summary}>
      <FindingList findings={perspective?.findings ?? []} />
    </SrbPanel>
  );
}

function PatternRecognitionPanel({
  perspective,
  knownPatterns,
}: {
  perspective: SrbPerspectiveReview | null;
  knownPatterns: SrbKnownPattern[];
}) {
  const historianFindings = (perspective?.findings ?? []).filter(
    (f) => !f.findingId.startsWith('HST-000'),
  );
  return (
    <SrbPanel title="Pattern Recognition" badge="Historian" summary={perspective?.summary}>
      {historianFindings.length > 0 && (
        <>
          <p className="mb-2 text-xs font-semibold uppercase tracking-[0.14em] text-white/40">
            Collective Historian Findings
          </p>
          <FindingList findings={historianFindings} />
        </>
      )}
      {knownPatterns.length > 0 && (
        <div className={historianFindings.length > 0 ? 'mt-4' : ''}>
          <p className="mb-2 text-xs font-semibold uppercase tracking-[0.14em] text-white/40">
            BioStack Known-Pattern Memory
          </p>
          <ul className="space-y-2">
            {knownPatterns.map((p) => (
              <li
                key={p.patternId}
                data-testid={`known-pattern-${p.patternId}`}
                className="rounded-lg border border-white/10 bg-black/20 p-3"
              >
                <p className="text-sm font-semibold text-white/85">{p.name}</p>
                <p className="mt-1 text-xs leading-5 text-white/55">{p.description}</p>
                <p className="mt-1 text-xs text-white/35">
                  Compounds: {p.matchedCompoundSlugs.join(', ')}
                </p>
              </li>
            ))}
          </ul>
        </div>
      )}
      {knownPatterns.length === 0 && historianFindings.length === 0 && (
        <p className="text-sm text-white/45">No pattern history matched this stack combination.</p>
      )}
    </SrbPanel>
  );
}

function ChallengePanel({ contradiction }: { contradiction: SrbContradictionReview | null }) {
  return (
    <SrbPanel title="Challenge This Stack">
      <div data-testid="challenge-panel" className="rounded-lg border border-white/10 bg-black/20 p-3">
        {contradiction ? (
          <>
            <p className="text-sm leading-6 text-white/72">{contradiction.counterPlanNarrative}</p>
            <div className="mt-3">
              <span
                data-testid="non-executable-counter-plan"
                className="rounded-full border border-amber-300/20 bg-amber-400/[0.08] px-2 py-0.5 text-xs font-semibold text-amber-100/65"
              >
                Non-executable counter-position
              </span>
            </div>
          </>
        ) : (
          <p className="text-sm text-white/45">No counter-position generated for this stack.</p>
        )}
      </div>
    </SrbPanel>
  );
}

function ConfidenceProfileSidebar({
  profile,
  graphRef,
}: {
  profile: SrbConfidenceProfile;
  graphRef: SrbCognitiveDensityEnvelope['reasoningGraphRef'];
}) {
  return (
    <section
      data-testid="confidence-profile-sidebar"
      className="rounded-lg border border-white/[0.08] bg-white/[0.03] p-4"
    >
      <p className="text-xs font-semibold uppercase tracking-[0.18em] text-white/42">
        Confidence Profile
      </p>
      <dl className="mt-3 space-y-2">
        <ProfileRow label="Epistemic" value={profile.epistemic} />
        <ProfileRow label="Evidence support" value={profile.evidenceSupport} />
        <ProfileRow label="Contradiction density" value={profile.contradictionDensity} />
        <ProfileRow label="Model" value={profile.model} />
        <ProfileRow label="Calibration" value={profile.calibrationVersion} />
      </dl>
      <div className="mt-4 border-t border-white/[0.06] pt-4">
        <p className="text-xs font-semibold uppercase tracking-[0.18em] text-white/42">
          Reasoning Graph
        </p>
        <p className="mt-2 text-xs text-white/50">
          {graphRef.nodeCount} nodes · {graphRef.edgeCount} edges
        </p>
        <p className="mt-1 font-mono text-xs text-white/30">{graphRef.graphId}</p>
      </div>
    </section>
  );
}

function ProfileRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex items-center justify-between gap-2 text-xs">
      <dt className="text-white/45">{label}</dt>
      <dd className="capitalize font-semibold text-white/75">{value}</dd>
    </div>
  );
}

function SrbLoadingSkeleton() {
  return (
    <div className="animate-pulse space-y-4" data-testid="srb-loading-skeleton">
      {Array.from({ length: 5 }).map((_, i) => (
        <div key={i} className="h-24 rounded-lg border border-white/[0.08] bg-white/[0.02]" />
      ))}
    </div>
  );
}
