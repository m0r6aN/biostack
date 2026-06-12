'use client';

import type { ProtocolAnalyzerArtifact, ProtocolAnalyzerResult } from '@/lib/types';
import {
  buildAnalyzerFindings,
  buildParserWarnings,
  confidenceLabel,
  sourceTypeLabel,
  type AnalyzerFinding,
} from '../analyzerView';

// ── Sub-components (moved verbatim from monolith) ─────────────────────────────

function TrustMetric({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-lg border border-white/10 bg-black/20 p-3">
      <p className="text-xs font-semibold uppercase tracking-[0.14em] text-white/40">{label}</p>
      <p className="mt-2 text-sm font-semibold text-white/82">{value}</p>
    </div>
  );
}

function ArtifactPreview({ artifact }: { artifact: ProtocolAnalyzerArtifact }) {
  return (
    <div className="rounded-lg border border-white/10 bg-black/20 p-3">
      <p className="text-xs font-semibold uppercase tracking-[0.14em] text-white/42">{artifact.label}</p>
      <p className="mt-2 text-sm leading-6 text-white/68">{artifact.preview}</p>
    </div>
  );
}

function FindingList({
  title,
  empty,
  findings,
}: {
  title: string;
  empty: string;
  findings: AnalyzerFinding[];
}) {
  const toneClasses = {
    positive: 'border-emerald-300/20 bg-emerald-400/[0.08] text-emerald-50',
    caution: 'border-amber-300/20 bg-amber-400/[0.08] text-amber-50',
    neutral: 'border-white/10 bg-black/18 text-white/72',
  };

  return (
    <section className="rounded-lg border border-white/[0.08] bg-white/[0.03] p-4">
      <h2 className="text-lg font-semibold text-white">{title}</h2>
      {findings.length > 0 ? (
        <ul className="mt-3 space-y-2.5">
          {findings.map((finding) => (
            <li key={`${finding.label}-${finding.message}`} className={`rounded-lg border p-3 ${toneClasses[finding.tone]}`}>
              <p className="text-xs font-semibold uppercase tracking-[0.14em] opacity-75">{finding.label}</p>
              <p className="mt-1 text-sm leading-6">{finding.message}</p>
            </li>
          ))}
        </ul>
      ) : (
        <p className="mt-3 text-sm leading-6 text-white/45">{empty}</p>
      )}
    </section>
  );
}

function ResultList({ title, empty, items }: { title: string; empty: string; items: string[] }) {
  return (
    <section className="rounded-lg border border-white/[0.08] bg-white/[0.03] p-4">
      <h2 className="text-lg font-semibold text-white">{title}</h2>
      {items.length > 0 ? (
        <ul className="mt-3 space-y-2">
          {items.map((item) => (
            <li key={item} className="flex gap-2 text-sm leading-6 text-white/68">
              <span aria-hidden="true" className="mt-1 shrink-0 text-amber-200/80">!</span>
              <span>{item}</span>
            </li>
          ))}
        </ul>
      ) : (
        <p className="mt-3 text-sm leading-6 text-white/45">{empty}</p>
      )}
    </section>
  );
}

// ── FindingsSection ────────────────────────────────────────────────────────────

export interface FindingsSectionProps {
  result: ProtocolAnalyzerResult;
  showExtractedText: boolean;
  onToggleExtractedText: () => void;
}

export function FindingsSection({ result, showExtractedText, onToggleExtractedText }: FindingsSectionProps) {
  const notes = [...result.extractionWarnings];
  if (result.lowConfidenceExtraction) {
    notes.unshift('Low-confidence extraction. Review the parsed protocol carefully before acting on it.');
  }
  const inferredCount = result.protocol.length;
  const normalizedCount = Math.max(0, result.protocol.length - result.unknownCompounds.length);
  const findings = buildAnalyzerFindings(result);
  const parserNotes = result.parserWarnings ?? buildParserWarnings(result);

  return (
    <div className="space-y-4">
      {/* Confidence / extraction notes strip */}
      <section className="rounded-lg border border-white/[0.08] bg-white/[0.03] p-4">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <h2 className="text-lg font-semibold text-white">What BioStack found</h2>
          <span className="rounded-full border border-white/10 px-3 py-1 text-xs font-semibold uppercase tracking-[0.16em] text-white/55">
            {result.inputType}
          </span>
        </div>

        {/* Trust metrics strip (3 TrustMetrics: source type, confidence, items inferred) */}
        <div className="mt-4 grid gap-3 sm:grid-cols-3">
          <TrustMetric label="Source type" value={sourceTypeLabel(result)} />
          <TrustMetric label="Confidence" value={confidenceLabel(result)} />
          <TrustMetric label="Items inferred" value={`${inferredCount} found, ${normalizedCount} normalized`} />
        </div>

        {/* Notes + artifact previews + extracted-text toggle */}
        <div className="mt-4 grid gap-4 lg:grid-cols-[1fr_1fr]">
          <div className="space-y-2 text-sm text-white/62">
            {result.sourceName ? <p>Source: {result.sourceName}</p> : null}
            {notes.length > 0
              ? notes.map((note) => <p key={note}>{note}</p>)
              : <p>Text extraction looked stable for this source.</p>}
            {result.artifacts.length > 0
              ? result.artifacts.slice(0, 3).map((artifact) => (
                  <ArtifactPreview key={`${artifact.kind}-${artifact.label}`} artifact={artifact} />
                ))
              : null}
          </div>
          <div className="rounded-lg border border-white/10 bg-black/20 p-3">
            <button
              type="button"
              onClick={onToggleExtractedText}
              className="text-xs font-semibold uppercase tracking-[0.16em] text-white/60 transition-colors hover:text-white"
            >
              {showExtractedText ? 'Hide extracted text' : 'View extracted text'}
            </button>
            {showExtractedText ? (
              <p className="mt-3 whitespace-pre-wrap text-sm leading-6 text-white/72">
                {result.extractedTextPreview || 'No extraction preview available.'}
              </p>
            ) : (
              <p className="mt-3 text-sm leading-6 text-white/45">
                Preview is available for review before converting this into a BioStack protocol.
              </p>
            )}
          </div>
        </div>
      </section>

      {/* Findings list + parser notes */}
      <div className="grid gap-4 md:grid-cols-2">
        <FindingList
          title="What BioStack found"
          empty="More findings are available in Operator."
          findings={findings}
        />
        <ResultList title="Parser notes" empty="Parser confidence looks clean." items={parserNotes} />
      </div>
    </div>
  );
}
